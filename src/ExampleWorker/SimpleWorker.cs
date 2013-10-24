using System.Net.Http;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleWorker : BaseWorker
    {
        protected override void Process()
        {
            var client = new HttpClient();
            client.GetAsync("http://blog.noocyte.net");
        }
    }
}