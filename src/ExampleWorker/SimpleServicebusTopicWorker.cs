using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleServicebusTopicWorker : BaseServiceBusTopicWorker
    {
        protected override string ConnectionString
        {
            get { return CloudConfigurationManager.GetSetting("ServiceBusConnectionString"); }
        }

        protected override string TopicName
        {
            get { return "SimpleTopic"; }
        }

        protected override async Task Do(string message)
        {
            var client = new HttpClient();
            await client.GetAsync("http://blog.noocyte.net").ConfigureAwait(false);
        }
    }
}