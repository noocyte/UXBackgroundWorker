using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Ninject.Extensions.Azure.Clients;

namespace Proactima.AzureWorkers
{
    public abstract class BaseEventHubProcessor : BaseWorker 
    {
        private readonly ICreateClients _clientFactory;
        private EventProcessorHost _host;

        protected BaseEventHubProcessor(ICreateClients clientFactory)
        {
            _clientFactory = clientFactory;
        }

        protected abstract string BaseHostName { get; }
        protected abstract string EventHubPath { get; }
        protected abstract IEventProcessorFactory ProcessorFactory { get; }

        protected virtual int MaxBatchSize
        {
            get { return 200; }
        }

        protected virtual TimeSpan ReceiveTimeout
        {
            get { return TimeSpan.FromSeconds(5); }
        }

        protected virtual string ConsumerGroupName
        {
            get { return EventHubConsumerGroup.DefaultGroupName; }
        }

        protected override async Task StartAsync()
        {
            InfoLogging("StartAsync called");

            if (_host == null)
                _host = _clientFactory.CreateEventProcessorHost(EventHubPath, BaseHostName, ConsumerGroupName);

            await _host
                .RegisterEventProcessorFactoryAsync(ProcessorFactory, CreateEventProcessorOptions())
                .ConfigureAwait(false);

            while (!Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
            }
            InfoLogging("StartAsync completes");
        }

        private EventProcessorOptions CreateEventProcessorOptions()
        {
            var eventProcessorOptions = new EventProcessorOptions
            {
                MaxBatchSize = MaxBatchSize,
                ReceiveTimeOut = ReceiveTimeout
            };
            return eventProcessorOptions;
        }

        protected override void OnStopping()
        {
            InfoLogging("Unregistering event processor");

            _host.UnregisterEventProcessorAsync().Wait();
        }
    }
}