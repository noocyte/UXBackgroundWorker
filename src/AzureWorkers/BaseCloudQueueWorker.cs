using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Proactima.AzureWorkers
{
    public abstract class BaseCloudQueueWorker : BaseWorker
    {
        private readonly ICreateClients _clientFactory;
        private CloudQueue _queue;

        public BaseCloudQueueWorker(ICreateClients clientFactory)
        {
            _clientFactory = clientFactory;
        }

        protected virtual int MessageCount
        {
            get { return 32; }
        }

        /// <summary>
        /// Will wait 5000 ms (5s) by default before requesting more messages.
        /// </summary>
        protected override int LoopWaitTime
        {
            get { return 5000; }
        }

        protected abstract string QueueName { get; }

        protected abstract Task Do(IEnumerable<CloudQueueMessage> messages);

        protected override async Task StartAsync()
        {
            InfoLogging(string.Format("{0} - Processing", QueueName));

            _queue = await _clientFactory.CreateStorageQueueClientAsync(QueueName).ConfigureAwait(false);

            var stopWatch = new Stopwatch();

            while (!Token.IsCancellationRequested)
            {
                await _queue.FetchAttributesAsync().ConfigureAwait(false);
                if (_queue.ApproximateMessageCount.GetValueOrDefault() <= 0)
                {
                    await Task.Delay(LoopWaitTime).ConfigureAwait(false);
                    continue;
                }

                var messages = await _queue
                    .GetMessagesAsync(MessageCount, TimeSpan.FromSeconds(10), null, null, Token)
                    .ConfigureAwait(false);

                var cloudQueueMessages = messages as IList<CloudQueueMessage> ?? messages.ToList();

                if (!cloudQueueMessages.Any()) continue;

                var correlationId = Guid.NewGuid().ToString();
                DebugLogging(string.Format("{0} - Received {1} new messages", QueueName, cloudQueueMessages.Count),
                    correlationId);
                stopWatch.Restart();

                await Do(cloudQueueMessages).ConfigureAwait(false);

                stopWatch.Stop();
                var timeSpan = stopWatch.Elapsed;
                DebugLogging(string.Format("{0} - Processed messages", QueueName), correlationId,
                    timeSpan.TotalSeconds);
            }
        }

        protected async Task DeleteMessages(IEnumerable<CloudQueueMessage> messages)
        {
            try
            {
                await Task
                    .WhenAll(messages.Select(msg => _queue.DeleteMessageAsync(msg, Token)))
                    .ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                ErrorLogging("Deleting messages failed", exception);
            }
        }
    }
}