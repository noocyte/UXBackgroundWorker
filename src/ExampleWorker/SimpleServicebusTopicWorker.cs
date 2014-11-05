using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Ninject.Extensions.Azure.Clients;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleServicebusTopicWorker : BaseServiceBusTopicWorker
    {
        private readonly List<string> _failedMessages = new List<string>();

        public SimpleServicebusTopicWorker(ICreateClientsAsync clientFactory)
            : base(clientFactory)
        {
            MessageRepostMaxCount = 4;
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

        protected override void DebugLogging(string message, string messageId = "", double timerValue = 0)
        {
            Debug.WriteLine("msg: {0}, timer: {1}", message, timerValue);
        }

        protected override void InfoLogging(string message, string messageId = "")
        {
            Debug.WriteLine("msg: {0}", message);
        }
    }
}