using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proactima.AzureWorkers
{
    public abstract class BaseWorker
    {
        private CancellationTokenSource _cancellationTokenSource;
        protected CancellationToken Token { get; set; }

        public virtual bool Enabled
        {
            get { return true; }
        }

        public virtual int NumberOfInstances
        {
            get { return 1; }
        }

        public int InstanceNumber { get; set; }

        protected virtual int LoopWaitTime
        {
            get { return 1000; }
        }

        protected abstract Task StartAsync();

        internal async Task ProtectedRun(CancellationTokenSource tokenSource)
        {
            try
            {
                _cancellationTokenSource = tokenSource;
                Token = _cancellationTokenSource.Token;

                while (!Token.IsCancellationRequested)
                {
                    await StartAsync().ConfigureAwait(false);
                    if (LoopWaitTime > 0)
                        Token.WaitHandle.WaitOne(LoopWaitTime);
                }
            }
            catch (Exception exception)
            {
                ErrorLogging("An exception was caught in BaseWorker.ProtectedRun", exception);
            }
        }

        internal void OnStop()
        {
            if (_cancellationTokenSource != null)
                _cancellationTokenSource.Cancel();
        }

        protected virtual void ErrorLogging(string message, Exception ex = null)
        {
        }

        protected virtual void ErrorLogging(string message, string messageId = "", Exception ex = null)
        {
        }

        protected virtual void InfoLogging(string message, string messageId = "")
        {
        }

        protected virtual void DebugLogging(string message, string messageId = "", double timerValue = 0.0)
        {
        }
    }
}