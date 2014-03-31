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

        protected override int LoopWaitTime
        {
            get { return 0; }
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

        protected virtual async Task Init()
        {
            var storageAccount = CloudStorageAccount.Parse(ConnectionString);
            var queueClient = storageAccount.CreateCloudQueueClient();
            _queue = queueClient.GetQueueReference(QueueName);
            await _queue.CreateIfNotExistsAsync().ConfigureAwait(false);
        }

        protected override async Task StartAsync()
        {
            InfoLogging(string.Format("{0} - Processing", SubscriptionName));

            if (_queue == null)
                await Init().ConfigureAwait(false);

// ReSharper disable once PossibleNullReferenceException
            await _queue.FetchAttributesAsync().ConfigureAwait(false);

            if (_queue.ApproximateMessageCount > 0 && !Token.IsCancellationRequested)
            {
                var messages =
                    await
                        _queue.GetMessagesAsync(MessageCount, TimeSpan.FromSeconds(30), null, null, Token)
                            .ConfigureAwait(false);

                var cloudQueueMessages = messages as IList<CloudQueueMessage> ?? messages.ToList();

                if (cloudQueueMessages.Any())
                {
                    try
                    {
                        await Do(cloudQueueMessages).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        ErrorLogging("Processing of messages failed", exception);
                    }
                }
            }
        }

        protected async Task DeleteMessages(IEnumerable<CloudQueueMessage> messages)
        {
            try
            {
                foreach (var msg in messages)
                    await _queue.DeleteMessageAsync(msg, Token).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                ErrorLogging("Deleting messages failed", exception);
            }
        }
    }
}