using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Proactima.AzureWorkers
{
    public abstract class BaseServiceBusQueueWorker : BaseServiceBusWorker
    {
        private QueueClient _queueClient;
        protected abstract string QueueName { get; }

        protected virtual int MessageRetrieveCount
        {
            get { return 100; }
        }

        protected virtual TimeSpan MessageRetrieveTimeout
        {
            get { return new TimeSpan(0, 0, 5); }
        }

        private string ImplementationName
        {
            get { return GetType().Name; }
        }

        protected abstract Task Do(IEnumerable<BrokeredMessage> messages);

        protected virtual async Task Init()
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            if (!await namespaceManager.QueueExistsAsync(QueueName))
                await namespaceManager.CreateQueueAsync(QueueName);

            _queueClient = QueueClient.CreateFromConnectionString(ConnectionString, QueueName);
        }

        public override async Task StartAsync()
        {
            await InfoLogging(string.Format("{0} - Processing", ImplementationName)).ConfigureAwait(false);

            if (_queueClient == null)
                await Init();

            if (_queueClient != null)
            {
                var messages =
                    await
                        _queueClient.ReceiveBatchAsync(MessageRetrieveCount, MessageRetrieveTimeout)
                            .ConfigureAwait(false);
                var brokeredMessages = messages as IList<BrokeredMessage> ?? messages.ToList();
                if (brokeredMessages.Any())
                    await Do(brokeredMessages).ConfigureAwait(false);
            }
        }

        protected async Task DeleteMessages(IEnumerable<BrokeredMessage> messages)
        {
            Func<Task> func = async () =>
            {
                await _queueClient
                    .CompleteBatchAsync(messages.Select(m => m.LockToken)).ConfigureAwait(false);
            };

            await func.LogWith(ErrorLogging, "Deleting messages failed").ConfigureAwait(false);
        }
    }
}