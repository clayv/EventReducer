using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CVV;

class Program
{
    const int ATTEMPTED_CALLS = 1000000;
    static void Main(string[] args)
    {
        Task[] tasks = new Task[ATTEMPTED_CALLS];
        Random rnd = new Random();
        EventHandler<Tests.MyEventArgs> m_EventHandler = null;

        using (var freqEvent = new EventReducer<Tests.MyEventArgs>(Tests.Tests.CalculatePrimes))
        {
            m_EventHandler += freqEvent.Handler;
            for (int attemptedCalls = 0; attemptedCalls < ATTEMPTED_CALLS; attemptedCalls++)
            {
                int max = rnd.Next(50000);
                //if (attemptedCalls == ATTEMPTED_CALLS >> 1)
                //{
                //    Console.WriteLine("Cancelling");
                //    freqEvent.Cancel();
                //}
                tasks[attemptedCalls] = Task.Run(() => m_EventHandler(null, new Tests.MyEventArgs() { Maximum = max }));
            }
            Task.WaitAll(tasks);
            Console.WriteLine("\nCompleted");
            Console.WriteLine("Press any to exit");
            Console.ReadKey();
        }
    }
}
