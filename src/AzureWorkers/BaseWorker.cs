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
            await Task.FromResult(0);
        }

        public virtual async Task<bool> OnStart(CancellationToken cancellationToken)
        {
            Token = cancellationToken;
            return await Task.FromResult(true);
        }

        internal async Task ProtectedRun()
        {
            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                Token = _cancellationTokenSource.Token;

                while (!Token.IsCancellationRequested)
                {
                    await StartAsync();
                    Token.WaitHandle.WaitOne(1000);
                }
            }
            catch (SystemException)
            {
                throw;
            }
// ReSharper disable EmptyGeneralCatchClause
            catch (Exception)
// ReSharper restore EmptyGeneralCatchClause
            {
            }
        }

        public virtual void OnStop()
        {
            _cancellationTokenSource.Cancel();
        }

        protected virtual async Task ErrorLogging(string message, Exception ex = null)
        {
            await Task.FromResult(0);
        }
        protected virtual async Task ErrorLogging(string message, string messageId = "", Exception ex = null)
        {
            await Task.FromResult(0);
        }

        protected virtual async Task InfoLogging(string message, string messageId = "")
        {
            await Task.FromResult(0);
        }

        protected virtual async Task DebugLogging(string message, string messageId = "", double timerValue = 0.0)
        {
            await Task.FromResult(0);
        }
    }
}