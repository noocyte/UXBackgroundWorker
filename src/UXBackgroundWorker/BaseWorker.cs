using System.Threading;

namespace UXBackgroundWorker
{
    public abstract class BaseWorker : IWorker
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ManualResetEvent _safeToExitHandle;

        protected abstract void Process();

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _safeToExitHandle = new ManualResetEvent(false);
            var token = _cancellationTokenSource.Token;

            while (!token.IsCancellationRequested)
            {
                this.Process();
                token.WaitHandle.WaitOne(1000);
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
