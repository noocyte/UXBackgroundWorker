using System;
using System.Threading;
using System.Threading.Tasks;

namespace Proactima.AzureWorkers
{
    public abstract class BaseWorker : IWorker
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

        public async Task ProtectedRun()
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

        public void OnStop()
        {
            _cancellationTokenSource.Cancel();
        }
    }
}