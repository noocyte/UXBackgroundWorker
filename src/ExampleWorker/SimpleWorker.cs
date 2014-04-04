using System.Net.Http;
using System.Threading.Tasks;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    [Instance(20)]
    public class SimpleWorker : BaseWorker
    {
        protected override async Task StartAsync()
        {
            var client = new HttpClient();

            if (!Token.IsCancellationRequested)
                await client.GetAsync("http://blog.noocyte.net").ConfigureAwait(false);
        }
    }
}