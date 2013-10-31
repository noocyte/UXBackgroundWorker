using System;
using System.Diagnostics;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;

namespace Proactima.AzureWorkers
{
    public abstract class BaseServiceBusWorker : BaseWorker
    {
        public BaseServiceBusWorker()
        {
            // ReSharper disable once DoNotCallOverridableMethodsInConstructor
            Init();
            MessageRepostMaxCount = 10;
        }

        /// <summary>
        /// Will by default get the value from ServiceBusConnectionString.
        /// Override to get it from somewhere else.
        /// </summary>
        protected virtual string ConnectionString
        {
            get
            {
                return CloudConfigurationManager.GetSetting("ServiceBusConnectionString");
            }
        }

        protected abstract string TopicName { get; }

        protected virtual Func<TimeSpan, BrokeredMessage> GetMessage { get; set; }
        protected virtual Action<BrokeredMessage> SendMessage { get; set; }

        protected int MessageRepostMaxCount { get; set; }

        private string SubscriptionName
        {
            get { return GetType().Name; }
        }

        protected abstract void Do(string message);

        protected virtual void ErrorLogging(string message, string messageId = "", Exception ex = null)
        {
        }

        protected virtual void InfoLogging(string message, string messageId = "")
        {
        }

        protected virtual void DebugLogging(string message, string messageId = "", double timerValue = 0.0)
        {
        }

        protected virtual void Init()
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(ConnectionString);

            if (!namespaceManager.TopicExists(TopicName))
                namespaceManager.CreateTopic(TopicName);

            if (!namespaceManager.SubscriptionExists(TopicName, SubscriptionName))
                namespaceManager.CreateSubscription(TopicName, SubscriptionName);

            // setup delegates to abstract away Service Bus stuff
            var subClient = SubscriptionClient.CreateFromConnectionString(ConnectionString, TopicName, SubscriptionName);
            GetMessage = subClient.Receive;

            var topicClient = TopicClient.CreateFromConnectionString(ConnectionString, TopicName);
            SendMessage = topicClient.Send;
        }

        protected override void Process()
        {
            InfoLogging(string.Format("{0} - Processing", SubscriptionName));

            BrokeredMessage message = null;

            while (message == null && !Token.IsCancellationRequested)
            {
                message = GetMessage(new TimeSpan(0, 0, 10));
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
                int messageCount = 0;

                if (counts.Length > 1)
                    Int32.TryParse(counts[1], out messageCount);

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                DebugLogging(string.Format("{0} - Received new message", SubscriptionName), message.MessageId);

                try
                {
                    Do(messageBody);
                }
                catch (Exception e)
                {
                    stopWatch.Stop();
                    ErrorLogging(string.Format("{0} - Failed to process message.", SubscriptionName), message.MessageId,
                        e);

                    if (messageCount < MessageRepostMaxCount)
                    {
                        messageCount++;

                        var appendedMessageBody = String.Format("{0}#{1}", messageBody, messageCount);
                        // this means that the message processing has failed and we need to repost the message
                        SendMessage(new BrokeredMessage(appendedMessageBody));
                        ErrorLogging(
                            string.Format("{0} - Reposting the message, retry #: {1}.", SubscriptionName, messageCount),
                            message.MessageId, e);
                    }
                    else
                    {
                        ErrorLogging(
                            string.Format("{0} - Done trying to repost message, message has failed to be processed.",
                                SubscriptionName));
                    }
                }

                if (stopWatch.IsRunning)
                    stopWatch.Stop();

                TimeSpan timeSpan = stopWatch.Elapsed;
                DebugLogging(string.Format("{0} - Processed message", SubscriptionName), message.MessageId,
                    timeSpan.TotalSeconds);
            }
        }
    }
}