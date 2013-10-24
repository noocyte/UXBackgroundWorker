using System.Net.Http;
using Microsoft.WindowsAzure;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleServicebusWorker : BaseServiceBusWorker
    {
        protected override string ConnectionString
        {
            get { return CloudConfigurationManager.GetSetting("TopicConnectionString"); }
        }

        protected override string TopicName
        {
            get { return "SimpleTopic"; }
        }

        protected override void Do(string message)
        {
            var client = new HttpClient();
            client.GetAsync("http://blog.noocyte.net");
        }
    }
}