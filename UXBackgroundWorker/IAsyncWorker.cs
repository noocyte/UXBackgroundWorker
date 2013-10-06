using System.Threading.Tasks;

namespace UXBackgroundWorker
{
    public interface IAsyncWorker
    {
        Task StartAsync();
        Task StopAsync();
    }
}
