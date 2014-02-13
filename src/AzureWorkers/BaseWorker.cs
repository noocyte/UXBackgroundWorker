using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proactima.AzureWorkers
{
    public abstract class BaseWorker
    {
        private CancellationTokenSource _cancellationTokenSource;
        protected CancellationToken Token { get; set; }

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
            Func<Task> loop = async () =>
            {
                _cancellationTokenSource = new CancellationTokenSource();
                Token = _cancellationTokenSource.Token;

                while (!Token.IsCancellationRequested)
                {
                    await StartAsync().ConfigureAwait(false);
                    if (LoopWaitTime > 0)
                        Token.WaitHandle.WaitOne(LoopWaitTime);
                }
            };

            await loop.LogWith(ErrorLogging,"An exception was caught in BaseWorker.ProtectedRun");
        }

        public virtual void OnStop()
        {
            _cancellationTokenSource.Cancel();
        }

        protected virtual async Task ErrorLogging(string message, Exception ex = null)
        {
            await Task.FromResult(0).ConfigureAwait(false);
        }
        protected virtual async Task ErrorLogging(string message, string messageId = "", Exception ex = null)
        {
            await Task.FromResult(0).ConfigureAwait(false);
        }

        protected virtual async Task InfoLogging(string message, string messageId = "")
        {
            await Task.FromResult(0).ConfigureAwait(false);
        }

        protected virtual async Task DebugLogging(string message, string messageId = "", double timerValue = 0.0)
        {
            await Task.FromResult(0).ConfigureAwait(false);
        }

        protected virtual int LoopWaitTime { get { return 1000; } }
    }
}