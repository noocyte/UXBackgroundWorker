using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Proactima.AzureWorkers
{
    public abstract class BaseServiceBusTopicWorker : BaseServiceBusWorker
    {
        protected BaseServiceBusTopicWorker()
        {
            MessageRepostMaxCount = 10;
        }

        protected abstract string TopicName { get; }

        protected virtual Func<TimeSpan, Task<BrokeredMessage>> GetMessage { get; set; }
        protected virtual Func<BrokeredMessage, Task> SendMessage { get; set; }

        protected int MessageRepostMaxCount { get; set; }

        private string SubscriptionName
        {
            get { return GetType().Name; }
        }

        protected abstract Task Do(string message);

        protected virtual async Task Init()
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            if (!await namespaceManager.TopicExistsAsync(TopicName).ConfigureAwait(false))
                await namespaceManager.CreateTopicAsync(TopicName).ConfigureAwait(false);

            if (!await namespaceManager.SubscriptionExistsAsync(TopicName, SubscriptionName).ConfigureAwait(false))
                await namespaceManager.CreateSubscriptionAsync(TopicName, SubscriptionName).ConfigureAwait(false);

            // setup delegates to abstract away Service Bus stuff
            var subClient = SubscriptionClient.CreateFromConnectionString(ConnectionString, TopicName, SubscriptionName);
            var topicClient = TopicClient.CreateFromConnectionString(ConnectionString, TopicName);

            GetMessage = subClient.ReceiveAsync;
            SendMessage = topicClient.SendAsync;
        }

        public override async Task StartAsync()
        {
            InfoLogging(string.Format("{0} - Processing", SubscriptionName));

            if (GetMessage == null || SendMessage == null)
                await Init().ConfigureAwait(false);

            BrokeredMessage message = null;

            while (message == null && !Token.IsCancellationRequested)
            {
// ReSharper disable once PossibleNullReferenceException
                message = await GetMessage(new TimeSpan(0, 0, 10)).ConfigureAwait(false);
            }

            if (message != null)
            {
                var messageBody = message.GetBody<string>();
                if (String.IsNullOrEmpty(messageBody))
                    messageBody = String.Empty;

                message.Complete();

                // try to extract a count
                var counts = messageBody.Split('#');
                messageBody = counts[0];
                var messageCount = 0;

                if (counts.Length > 1)
                    Int32.TryParse(counts[1], out messageCount);

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                DebugLogging(string.Format("{0} - Received new message", SubscriptionName), message.MessageId);
                var repostMessage = false;

                try
                {
                    await Do(messageBody).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    stopWatch.Stop();

                    ErrorLogging(string.Format("{0} - Failed to process message.", SubscriptionName), message.MessageId,
                        e);

                    if (messageCount < MessageRepostMaxCount)
                    {
                        InfoLogging(
                            string.Format("{0} - Reposting the message, retry #: {1}.", SubscriptionName, messageCount),
                            message.MessageId);
                        repostMessage = true;
                    }
                    else
                    {
                        InfoLogging(
                            string.Format("{0} - Done trying to repost message, message has failed to be processed.",
                                SubscriptionName));
                    }
                }

                if (stopWatch.IsRunning)
                    stopWatch.Stop();

                var timeSpan = stopWatch.Elapsed;
                DebugLogging(string.Format("{0} - Processed message", SubscriptionName), message.MessageId,
                    timeSpan.TotalSeconds);

                if (repostMessage)
                    await RepostMessage(messageCount, messageBody).ConfigureAwait(false);
            }
        }

        private async Task RepostMessage(int messageCount, string messageBody)
        {
            messageCount++;

            var appendedMessageBody = String.Format("{0}#{1}", messageBody, messageCount);
            await SendMessage(new BrokeredMessage(appendedMessageBody)).ConfigureAwait(false);
        }
    }
}