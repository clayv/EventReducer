using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CVV;

class ConsoleAppArgs : ReducedEventArgs
{
    public int Argument { get; set; }
}

class Program
{
    const int ATTEMPTED_CALLS = 1000000;
    static void Main(string[] args)
    {
        Task[] tasks = new Task[ATTEMPTED_CALLS];
        Random rnd = new Random();
        EventHandler<ConsoleAppArgs> m_EventHandler = null;

        using (var freqEvent = new EventReducer<ConsoleAppArgs>((o, eventArgs) =>
        {
            Console.WriteLine($"Handled event request: {(int)o}\t with argument {eventArgs.Argument}");
            Thread.Sleep(500);
        }))
        {
            m_EventHandler += freqEvent.Handler;
            for (int attemptedCalls = 1; attemptedCalls <= ATTEMPTED_CALLS; attemptedCalls++)
            {
                int request = attemptedCalls;
                tasks[attemptedCalls - 1] = Task.Run(() => m_EventHandler(request, new ConsoleAppArgs() { Argument = request }));
            }
            Task.WaitAll(tasks);
            Console.WriteLine("\nCompleted");
            Console.WriteLine("Press any to exit");
            Console.ReadKey();
        }
    }
}
