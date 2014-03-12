using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proactima.AzureWorkers
{
    public abstract class BaseWorker
    {
        private CancellationTokenSource _cancellationTokenSource;
        protected CancellationToken Token { get; set; }

        public virtual bool Enabled { get { return true; } }

        public virtual async Task StartAsync()
        {
            await Task.FromResult(0).ConfigureAwait(false);
        }

        public virtual async Task<bool> OnStart(CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            return await Task.FromResult(true).ConfigureAwait(false);
        }

        internal async Task ProtectedRun()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
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

        public virtual void OnStop()
        {
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

        protected virtual int LoopWaitTime { get { return 1000; } }
    }
}