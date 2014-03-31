using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleWorker : BaseWorker
    {
        public override int NumberOfInstances
        {
            get { return 10; }
        }

        protected override async Task StartAsync()
        {
            Debug.WriteLine("SimpleWorker #" + InstanceNumber);
            var client = new HttpClient();

            if (!Token.IsCancellationRequested)
                await client.GetAsync("http://blog.noocyte.net").ConfigureAwait(false);
        }
    }
}