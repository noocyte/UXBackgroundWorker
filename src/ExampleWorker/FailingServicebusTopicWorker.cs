using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class FailingServicebusTopicWorker : BaseServiceBusTopicWorker
    {
        private readonly List<string> _failedMessages = new List<string>();

        protected override string ConnectionString
        {
            get { return CloudConfigurationManager.GetSetting("ServiceBusConnectionString"); }
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