using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;

namespace Proactima.AzureWorkers
{
    public abstract class BaseServiceBusWorker : BaseWorker
    {
        protected BaseServiceBusWorker()
        {
            MessageRepostMaxCount = 10;
        }

        /// <summary>
        /// Will by default get the value from ServiceBusConnectionString.
        /// Override to get it from somewhere else.
        /// </summary>
        protected virtual string ConnectionString
        {
            get { return CloudConfigurationManager.GetSetting("ServiceBusConnectionString"); }
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

            if (!await namespaceManager.TopicExistsAsync(TopicName))
                await namespaceManager.CreateTopicAsync(TopicName);

            if (!await namespaceManager.SubscriptionExistsAsync(TopicName, SubscriptionName))
                await namespaceManager.CreateSubscriptionAsync(TopicName, SubscriptionName);

            // setup delegates to abstract away Service Bus stuff
            var subClient = SubscriptionClient.CreateFromConnectionString(ConnectionString, TopicName, SubscriptionName);
            var topicClient = TopicClient.CreateFromConnectionString(ConnectionString, TopicName);

            GetMessage = subClient.ReceiveAsync;
            SendMessage = topicClient.SendAsync;
        }

        public override async Task StartAsync()
        {
            await InfoLogging(string.Format("{0} - Processing", SubscriptionName)).ConfigureAwait(false);

            if (GetMessage == null || SendMessage == null)
                await Init();

            BrokeredMessage message = null;

            while (message == null && !Token.IsCancellationRequested)
            {
                message = await GetMessage(new TimeSpan(0, 0, 10));
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
                await
                    DebugLogging(string.Format("{0} - Received new message", SubscriptionName), message.MessageId)
                        .ConfigureAwait(false);
                var repostMessage = false;

                Func<Task> action = async () => { await Do(messageBody).ConfigureAwait(false); };

                await action.HandleExceptionWith(async (e) =>
                {
                    stopWatch.Stop();

                    await
                        ErrorLogging(string.Format("{0} - Failed to process message.", SubscriptionName),
                            message.MessageId,
                            e).ConfigureAwait(false);

                    if (messageCount < MessageRepostMaxCount)
                    {
                        await InfoLogging(
                            string.Format("{0} - Reposting the message, retry #: {1}.", SubscriptionName, messageCount),
                            message.MessageId).ConfigureAwait(false);
                        repostMessage = true;
                    }
                    else
                    {
                        await InfoLogging(
                            string.Format("{0} - Done trying to repost message, message has failed to be processed.",
                                SubscriptionName)).ConfigureAwait(false);
                    }
                });

                if (stopWatch.IsRunning)
                    stopWatch.Stop();

                if (repostMessage)
                    await RepostMessage(messageCount, messageBody);

                var timeSpan = stopWatch.Elapsed;
                await DebugLogging(string.Format("{0} - Processed message", SubscriptionName), message.MessageId,
                    timeSpan.TotalSeconds).ConfigureAwait(false);
            }
        }

        private async Task RepostMessage(int messageCount, string messageBody)
        {
            messageCount++;

            var appendedMessageBody = String.Format("{0}#{1}", messageBody, messageCount);
            await SendMessage(new BrokeredMessage(appendedMessageBody));
        }
    }
}