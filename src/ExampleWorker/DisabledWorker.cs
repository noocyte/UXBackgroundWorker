using System.Net.Http;
using System.Threading.Tasks;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class DisabledWorker : BaseWorker
    {
        public override bool Enabled
        {
            get { return false; }
        }

        public override async Task StartAsync()
        {
            // will never be called!
            var client = new HttpClient();
            await client.GetAsync("http://somebodysblog.com").ConfigureAwait(false);
        }
    }
}