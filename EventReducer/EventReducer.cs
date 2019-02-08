using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CVV
{
    public class EventReducer<T> : Disposable where T : IReducedEventArgs
    {
        enum State
        {
            Waiting,
            Running,
            Queued
        }

#if DEBUG
        private long m_AttemptedCalls, m_ActualCalls, m_IgnoredCalls;
#endif

        private readonly AutoResetEvent m_SingleEntry = new AutoResetEvent(true);
        private readonly CancellationTokenSource m_TokenSource = new CancellationTokenSource();
        private readonly ConcurrentQueue<T> m_ArgQueue = new ConcurrentQueue<T>();
        private readonly Action<object, T> m_Action;

        private int m_CurrentState = (int)State.Waiting;

        public EventReducer(Action<object, T> handler)
        {
            m_Action = handler ?? throw new ArgumentNullException("handler", "Reduced event delegate cannot be null.");
        }

        public void Cancel() => m_TokenSource.Cancel();

        public async void Handler(object sender, T args)
        {
            bool needDequeue;
            T queuedArg;

#if DEBUG
            Interlocked.Increment(ref m_AttemptedCalls);
#endif
            int state = Interlocked.Increment(ref m_CurrentState);
            try
            {
                m_ArgQueue.Enqueue(args);
                needDequeue = true;
                if (state <= (int)State.Queued) // Previous state was WAITING or RUNNING
                {
                    try
                    {
                        m_SingleEntry.WaitOne(); // Ensure only one run at a time
                        if (!Disposed && !m_TokenSource.IsCancellationRequested)
                        {
#if DEBUG
                            m_ActualCalls++;
#endif
                            if (m_ArgQueue.TryDequeue(out queuedArg))
                            {
                                needDequeue = false;
                                args = queuedArg;
                            }
                            (args as ReducedEventArgs).CancellationToken = m_TokenSource.Token;
                            // Execute CPU intensive task
                            await Task.Run(() => m_Action(sender, args), m_TokenSource.Token);
                        }
                    }
                    catch (AggregateException ae)
                    {
                        foreach(Exception e in ae.InnerExceptions.Where(i => !(i is TaskCanceledException)))
                        {
                            throw e;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        /******************************************************************************************\
                        | This will occur rarely, but could and particularly when trying to get or pass the value  |
                        | of m_TokenSource.Token. If it does happen we do not want to try to call the actual       |
                        | handler any more anyway, so eat the exception and let finally block perform any cleanup. |
                        \******************************************************************************************/
                    }
                    finally
                    {
                        if (!Disposed)
                        {
                            // Allow a waiting thread to proceed
                            m_SingleEntry.Set();
                        }
                        if (needDequeue)
                        {
                            m_ArgQueue.TryDequeue(out queuedArg);
                        }
                    }
                }
                else
                {
                    m_ArgQueue.TryDequeue(out queuedArg);
#if DEBUG
                    Interlocked.Increment(ref m_IgnoredCalls);
#endif
                }
            }
            finally
            {
                Interlocked.Decrement(ref m_CurrentState);
            }
        }

        protected override void CleanUpResources()
        {
            try
            {
#if DEBUG
                Trace.WriteLine($"EventReducer {GetHashCode()} made {m_ActualCalls} call(s) out of {m_AttemptedCalls} requested and ignored {m_IgnoredCalls}" );
                Trace.WriteLine($"Argument queue had {m_ArgQueue.Count} items remaining.");
#endif
                if (!m_TokenSource.IsCancellationRequested)
                {
                    Cancel();
                }
                m_TokenSource.Dispose();
                m_SingleEntry.Dispose();
            }
            finally
            {
                base.CleanUpResources();
            }
        }
    }
}
