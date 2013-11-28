using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Proactima.AzureWorkers
{
    public abstract class BaseQueueWorker : BaseWorker
    {
        private CloudQueue _queue;

        protected virtual int MessageCount
        {
            get { return 32; }
        }

        protected virtual string ConnectionString
        {
            get { return CloudConfigurationManager.GetSetting("StorageConnectionString"); }
        }

        protected abstract string QueueName { get; }

        private string SubscriptionName
        {
            get { return GetType().Name; }
        }

        protected abstract Task Do(IEnumerable<CloudQueueMessage> messages);

        protected virtual async Task ErrorLogging(string message, Exception ex = null)
        {
            await Task.FromResult(0);
        }

        protected virtual async Task InfoLogging(string message)
        {
            await Task.FromResult(0);
        }

        protected virtual async Task DebugLogging(string message, double timerValue = 0.0)
        {
            await Task.FromResult(0);
        }

        protected virtual async Task Init()
        {
            var storageAccount = CloudStorageAccount.Parse(ConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference(QueueName);
            await _queue.CreateIfNotExistsAsync();
        }

        public override async Task StartAsync()
        {
            await InfoLogging(string.Format("{0} - Processing", SubscriptionName)).ConfigureAwait(false);

            if (_queue == null)
                await Init();

            await _queue.FetchAttributesAsync().ConfigureAwait(false);

            if (_queue.ApproximateMessageCount > 0 && !Token.IsCancellationRequested)
            {
                var messages =
                    await
                        _queue.GetMessagesAsync(MessageCount, TimeSpan.FromSeconds(30), null, null, Token)
                            .ConfigureAwait(false);

                if (messages.Any())
                {
                    Func<Task> func = async () => { await Do(messages).ConfigureAwait(false); };

                    await func.LogWith(ErrorLogging, "Processing of messages failed").ConfigureAwait(false);
                }
            }
        }

        protected async Task DeleteMessages(IEnumerable<CloudQueueMessage> messages)
        {
            Func<Task> func = async () =>
            {
                foreach (var msg in messages)
                {
                    await _queue.DeleteMessageAsync(msg, Token).ConfigureAwait(false);
                }
            };

            await func.LogWith(ErrorLogging, "Deleting messages failed").ConfigureAwait(false);
        }
    }
}