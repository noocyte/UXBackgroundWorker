using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Proactima.AzureWorkers
{
    public abstract class BaseServiceBusQueueWorker : BaseServiceBusWorker
    {
        private readonly ICreateClients _clientFactory;
        private QueueClient _queueClient;

        public BaseServiceBusQueueWorker(ICreateClients clientFactory)
        {
            _clientFactory = clientFactory;
        }

        protected abstract string QueueName { get; }

        protected virtual int MessageRetrieveCount
        {
            get { return 100; }
        }

        protected virtual TimeSpan MessageRetrieveTimeout
        {
            get { return new TimeSpan(0, 0, 5); }
        }

        protected abstract Task Do(IEnumerable<BrokeredMessage> messages);

        protected override async Task StartAsync()
        {
            InfoLogging(string.Format("{0} - Processing", QueueName));

            _queueClient = await _clientFactory.CreateServicebusQueueClientAsync(QueueName).ConfigureAwait(false);

            var stopWatch = new Stopwatch();

            while (!Token.IsCancellationRequested)
            {
                var messages = await _queueClient
                    .ReceiveBatchAsync(MessageRetrieveCount, MessageRetrieveTimeout)
                    .ConfigureAwait(false);
                var brokeredMessages = messages as IList<BrokeredMessage> ?? messages.ToList();
                if (!brokeredMessages.Any()) continue;

                var correlationId = Guid.NewGuid().ToString();
                DebugLogging(string.Format("{0} - Received {1} new messages", QueueName, brokeredMessages.Count),
                    correlationId);

                stopWatch.Restart();
                await Do(brokeredMessages).ConfigureAwait(false);

                stopWatch.Stop();
                var timeSpan = stopWatch.Elapsed;
                DebugLogging(string.Format("{0} - Processed messages", QueueName), correlationId,
                    timeSpan.TotalSeconds);
            }
        }

        protected async Task DeleteMessages(IEnumerable<BrokeredMessage> messages)
        {
            if (_queueClient == null) return;
            
            try
            {
                await _queueClient
                    .CompleteBatchAsync(messages.Select(m => m.LockToken))
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                ErrorLogging("Deleting messages failed", exception);
                throw;
            }
        }
    }
}