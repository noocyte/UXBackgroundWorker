using System.Net.Http;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleStartupTask : IStartupTask
    {
        public void Start()
        {
            var client = new HttpClient();
            client.GetAsync("http://blog.noocyte.net");
        }
    }
}
