using System.Net.Http;
using System.Threading.Tasks;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleWorker : BaseWorker
    {
        public override async Task StartAsync()
        {
            var client = new HttpClient();
            await client.GetAsync("http://blog.noocyte.net").ConfigureAwait(false);
        }
    }
}