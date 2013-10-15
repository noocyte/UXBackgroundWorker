using System.Threading;

namespace UXBackgroundWorker
{
    public abstract class BaseWorker : IWorker
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ManualResetEvent _safeToExitHandle;

        protected CancellationToken Token { get; set; }

        protected abstract void Process();

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _safeToExitHandle = new ManualResetEvent(false);
            this.Token = _cancellationTokenSource.Token;

            while (!this.Token.IsCancellationRequested)
            {
                this.Process();
                this.Token.WaitHandle.WaitOne(1000);
            }
            _safeToExitHandle.Set();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _safeToExitHandle.WaitOne();
        }
    }
}
