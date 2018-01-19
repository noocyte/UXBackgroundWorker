using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;
using Ninject.Extensions.Azure.Clients;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleCloudQueueWorker : BaseCloudQueueWorker
    {
        public SimpleCloudQueueWorker(ICreateClientsAsync clientFactory)
            : base(clientFactory)
        {
        }

        protected override string QueueName
        {
            get { return "dummy"; }
        }

        protected override async Task Do(IEnumerable<CloudQueueMessage> messages)
        {
            await DeleteMessages(messages).ConfigureAwait(false);
        }
    }
}