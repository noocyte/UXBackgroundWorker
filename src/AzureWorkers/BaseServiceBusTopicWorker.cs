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

                var stopWatch = new Stopwatch();
                stopWatch.Start();
                DebugLogging(string.Format("{0} - Received new message", SubscriptionName), message.MessageId);

                try
                {
                    await Do(messageBody).ConfigureAwait(false);
                }
                finally
                {
                    if (stopWatch.IsRunning)
                        stopWatch.Stop();

                    var timeSpan = stopWatch.Elapsed;
                    DebugLogging(string.Format("{0} - Processed message", SubscriptionName), message.MessageId,
                        timeSpan.TotalSeconds);   
                }
            }
        }
    }
}