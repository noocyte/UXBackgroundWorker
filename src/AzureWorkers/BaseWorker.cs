using System.Threading;

namespace Proactima.AzureWorkers
{
    public abstract class BaseWorker : IWorker
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ManualResetEvent _safeToExitHandle;

        protected CancellationToken Token { get; set; }

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _safeToExitHandle = new ManualResetEvent(false);
            Token = _cancellationTokenSource.Token;

            while (!Token.IsCancellationRequested)
            {
                Process();
                Token.WaitHandle.WaitOne(1000);
            }
            _safeToExitHandle.Set();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _safeToExitHandle.WaitOne();
        }

        public virtual int NumberOfInstances { get { return 1; } }

        protected abstract void Process();
    }
}