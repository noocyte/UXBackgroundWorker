using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Ninject.Extensions.Azure.Clients;

namespace Proactima.AzureWorkers
{
    public abstract class BaseEventHubProcessor<T> : BaseWorker where T : IEventProcessor
    {
        private readonly ICreateClients _clientFactory;
        private EventProcessorHost _host;

        public BaseEventHubProcessor(ICreateClients clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public abstract string BaseHostName { get; }
        public abstract string EventHubPath { get; }

        public virtual int MaxBatchSize
        {
            get { return 200; }
        }

        public virtual TimeSpan ReceiveTimeout
        {
            get { return TimeSpan.FromSeconds(5); }
        }

        public virtual string ConsumerGroupName
        {
            get { return EventHubConsumerGroup.DefaultGroupName; }
        }

        protected override async Task StartAsync()
        {
            InfoLogging("StartAsync called");

            if (_host == null)
                _host = _clientFactory.CreateEventProcessorHost(EventHubPath, BaseHostName, ConsumerGroupName);
            else // IF we loop out and come in again, make sure to shut down the host first
                await _host.UnregisterEventProcessorAsync().ConfigureAwait(false);

            await _host
                .RegisterEventProcessorAsync<T>(CreateEventProcessorOptions())
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