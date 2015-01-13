using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Ninject.Extensions.Azure.Clients;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleProcessor : IEventProcessor
    {
        public Task OpenAsync(PartitionContext context)
        {
            return Task.FromResult(0);
        }

        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            messages
                .Select(message => message.GetBodyStream())
                .Select(LogEntry.FromStream)
                .ToList()
                .ForEach(entry => DebugWrite(context, entry));
            
            return Task.FromResult(0);
        }

        private static void DebugWrite(PartitionContext context, LogEntry entry)
        {
            Debug.WriteLine("Partition: {0}, Message: {1}", context.Lease.PartitionId, entry.Message);
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            await context.CheckpointAsync();
        }
    }

    public class SimpleEventHubHost : BaseEventHubProcessor<SimpleProcessor>
    {
        public SimpleEventHubHost(ICreateClients clientFactory) : base(clientFactory)
        {
        }

        public override string BaseHostName
        {
            get { return "simpletest"; }
        }

        public override string EventHubPath
        {
            get { return "simpletest"; }
        }
    }
}