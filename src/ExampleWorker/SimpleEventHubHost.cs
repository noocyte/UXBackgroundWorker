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

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            await context.CheckpointAsync();
        }

        private static void DebugWrite(PartitionContext context, LogEntry entry)
        {
            Debug.WriteLine("Partition: {0}, Message: {1}", context.Lease.PartitionId, entry.Message);
        }
    }

    public sealed class SimpleEventHubHost : BaseEventHubProcessor
    {
        private readonly IEventProcessorFactory _processorFactory;

        public SimpleEventHubHost(ICreateClients clientFactory) : base(clientFactory)
        {
            _processorFactory = new SimpleProcessorFactory();
        }

        protected override string BaseHostName
        {
            get { return "simpletest"; }
        }

        protected override string EventHubPath
        {
            get { return "simpletest"; }
        }

        protected override IEventProcessorFactory ProcessorFactory
        {
            get { return _processorFactory; }
        }

        private class SimpleProcessorFactory : IEventProcessorFactory
        {
            public IEventProcessor CreateEventProcessor(PartitionContext context)
            {
                return new SimpleProcessor();
            }
        }
    }
}