using System.Threading;
using System.Threading.Tasks;

namespace Proactima.AzureWorkers
{
    public interface IWorker
    {
        Task ProtectedRun();
        Task<bool> OnStart(CancellationToken cancellationToken);
        Task StartAsync();
        void OnStop();
    }
}