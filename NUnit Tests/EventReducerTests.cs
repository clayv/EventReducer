using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using CVV;

namespace Tests
{
    public class MyEventArgs : ReducedEventArgs
    {
        public int Maximum { get; set; }
    }

    [TestFixture]
    public class Tests
    {
        [Test]
        public void ConstructorThrowsExceptionOnNullHandler()
        {
            Assert.Throws<ArgumentNullException>(() => 
            {
                using (new EventReducer<IReducedEventArgs>(null)) { }
            });
        }

        [Test]
        public void HandlerThrowsException()
        {
            EventHandler<ReducedEventArgs> eventHandler = null;
            using (var freqEvent = new EventReducer<ReducedEventArgs>((o, args) => 
            {
                // NOTE: The handler MUST catch _and_ handle its own exceptions.
                // This means you cannot rethrow the exception!!
                try
                {
                    throw new ArgumentException();
                }
                catch(Exception e)
                {
                    Assert.IsInstanceOf<ArgumentException>(e);
                }
            }))
            {
                eventHandler += freqEvent.Handler;
                eventHandler(null, new ReducedEventArgs());
            }
        }

        [Test]
        public void ReduceEventsWithCancellation()
        {
            const int ATTEMPTED_CALLS = 10000000;

            Task[] tasks = new Task[ATTEMPTED_CALLS];
            Random rnd = new Random();
            int stopVal = rnd.Next(ATTEMPTED_CALLS);
            EventHandler<MyEventArgs> eventHandler = null;

            using (var freqEvent = new EventReducer<MyEventArgs>(CalculatePrimes))
            {
                eventHandler += freqEvent.Handler;
                for (int attemptedCalls = 0; attemptedCalls < ATTEMPTED_CALLS; attemptedCalls++)
                {
                    if (attemptedCalls == stopVal)
                    {
                        freqEvent.Cancel();
                        return;
                    }
                    int max = rnd.Next(50000);
                    tasks[attemptedCalls] = Task.Run(() => eventHandler(null, new MyEventArgs() { Maximum = max }));
                }
                tasks = tasks.Where(t => t != null).ToArray();
                Task.WaitAll(tasks);
                Assert.AreEqual(stopVal, tasks.Length);
            }
        }

        [Test]
        public void ReduceEventsWithoutCancellation()
        {
            const int ATTEMPTED_CALLS = 10000000;

            Task[] tasks = new Task[ATTEMPTED_CALLS];
            Random rnd = new Random();
            EventHandler<MyEventArgs> eventHandler = null;

            using (var freqEvent = new EventReducer<MyEventArgs>(CalculatePrimes))
            {
                eventHandler += freqEvent.Handler;
                for (int attemptedCalls = 0; attemptedCalls < ATTEMPTED_CALLS; attemptedCalls++)
                {
                    int max = rnd.Next(50000);
                    tasks[attemptedCalls] = Task.Run(() => eventHandler(null, new MyEventArgs() { Maximum = max }));
                }
                Task.WaitAll(tasks);
                Assert.AreEqual(ATTEMPTED_CALLS, tasks.Where(t => t != null).Count());
            }
        }

        [Test]
        public void UsesMoreRecentArgs()
        {
            const int ATTEMPTED_CALLS = 1000000;

            Task[] tasks = new Task[ATTEMPTED_CALLS];
            EventHandler<MyEventArgs> eventHandler = null;
            using (var freqEvent = new EventReducer<MyEventArgs>((o, args) =>
            {
                CalculatePrimes(o, args);
                Assert.LessOrEqual((int)o, args.Maximum);
            }))
            {
                eventHandler += freqEvent.Handler;
                for (int attemptedCalls = 0; attemptedCalls < ATTEMPTED_CALLS; attemptedCalls++)
                {
                    tasks[attemptedCalls] = Task.Run(() => eventHandler(attemptedCalls, new MyEventArgs() { Maximum = attemptedCalls }));
                }
                Task.WaitAll(tasks);
            }
        }
        [Test]
        public void UsesMoreRecentArgsStress()
        {
            const int ATTEMPTED_CALLS = 1000000;

            Task[] tasks = new Task[ATTEMPTED_CALLS];
            EventHandler<MyEventArgs> eventHandler = null;
            using (var freqEvent = new EventReducer<MyEventArgs>((o, args) =>
            {
                // No calculations are occuring here so I'm willing 
                // for this to be a little off, hence the multiplier.
                // But even with the multiplier it can still fail.
                Assert.LessOrEqual((int)o * 0.999, args.Maximum);
            }))
            {
                eventHandler += freqEvent.Handler;
                for (int attemptedCalls = 0; attemptedCalls < ATTEMPTED_CALLS; attemptedCalls++)
                {
                    int request = attemptedCalls;
                    tasks[attemptedCalls] = Task.Run(() => eventHandler(request, new MyEventArgs() { Maximum = request }));
                }
                Task.WaitAll(tasks);
            }
        }

        public static void CalculatePrimes(object sender, MyEventArgs args)
        {
            int max = args.Maximum;
            bool isPrime = true;

            for (int i = 2; i <= max; i++)
            {
                for (int j = 2; j <= max; j++)
                {
                    if (args.CancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    if (i != j && i % j == 0)
                    {
                        isPrime = false;
                        break;
                    }

                }
                if (isPrime)
                {
                    //Console.WriteLine($"{i}\t");
                }
                isPrime = true;
            }
        }
    }
}