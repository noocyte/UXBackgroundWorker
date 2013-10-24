
namespace Proactima.AzureWorkers
{
    public interface IWorker
    {
        void Start();
        void Stop();
        int NumberOfInstances { get; }
    }
}
