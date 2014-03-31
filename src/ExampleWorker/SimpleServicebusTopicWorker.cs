using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleServicebusTopicWorker : BaseServiceBusTopicWorker
    {
        private readonly List<string> _failedMessages = new List<string>();

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
            await client.GetAsync("http://blog.uxrisk.com").ConfigureAwait(false);
        }

        protected override async Task HandleFailedMessageAsync(string messageBody)
        {
            await Task.Factory.StartNew(() => _failedMessages.Add(messageBody));
        }
    }
}