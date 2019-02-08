using System;
using System.Threading;

namespace CVV
{
    public interface IReducedEventArgs
    {
        CancellationToken CancellationToken { get; }
    }

    public class ReducedEventArgs : EventArgs, IReducedEventArgs
    {
        public CancellationToken CancellationToken { get; internal set; }
    }
}
