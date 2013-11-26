using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        protected virtual void ErrorLogging(string message, Exception ex = null)
        {
        }

        protected virtual void InfoLogging(string message)
        {
        }

        protected virtual void DebugLogging(string message, double timerValue = 0.0)
        {
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
            InfoLogging(string.Format("{0} - Processing", SubscriptionName));
           
            if (_queue == null)
                await Init();

            await _queue.FetchAttributesAsync();

            if (_queue.ApproximateMessageCount > 0 && !Token.IsCancellationRequested)
            {
                var messages = await _queue.GetMessagesAsync(MessageCount, TimeSpan.FromSeconds(30), null, null, Token);

                if (messages.Any())
                {
                    try
                    {
                        await Do(messages);
                    }
                    catch (Exception ex)
                    {
                        ErrorLogging("Processing of messages failed", ex);
                    }
                }
            }
        }

        protected async Task DeleteMessages(IEnumerable<CloudQueueMessage> messages)
        {
            try
            {
                foreach (var msg in messages)
                {
                    await _queue.DeleteMessageAsync(msg, Token);
                }
            }
            catch (Exception ex)
            {
                ErrorLogging("Deleting messages failed", ex);
            }
        }
    }
}