using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleServiceBusQueueWorker : BaseServiceBusQueueWorker
    {
        protected override string QueueName
        {
            get { return "RandomQueue"; }
        }

        protected override async Task Do(IEnumerable<BrokeredMessage> messages)
        {
            await DeleteMessages(messages);
        }
    }
}