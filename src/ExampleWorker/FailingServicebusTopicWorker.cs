using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ninject.Extensions.Azure.Clients;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class FailingServicebusTopicWorker : BaseServiceBusTopicWorker
    {
        private readonly List<string> _failedMessages = new List<string>();

        public FailingServicebusTopicWorker(ICreateClientsAsync clientFactory)
            : base(clientFactory)
        {
        }

        protected override string TopicName
        {
            get { return "FailingTopic"; }
        }

        protected override Task Do(string message)
        {
            throw new NotImplementedException("This will always fail!");
        }

        protected override async Task HandleFailedMessageAsync(string messageBody)
        {
            await Task.Factory.StartNew(() => _failedMessages.Add(messageBody));
        }
    }
}