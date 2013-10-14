using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Diagnostics;

namespace UXBackgroundWorker
{
    public abstract class BaseServiceBusWorker : BaseWorker
    {
        protected abstract string ConnectionString { get; }
        protected abstract void Do(string message);
        protected abstract string TopicName { get; }

        protected virtual void ErrorLogging(string message, string messageId = "", Exception ex = null) { }
        protected virtual void InfoLogging(string message, string messageId = "") { }
        protected virtual void DebugLogging(string message, string messageId = "", double timerValue = 0.0) { }

        protected virtual Func<BrokeredMessage> GetMessage { get; set; }
        protected virtual Action<BrokeredMessage> SendMessage { get; set; }

        protected int MessageRepostMaxCount { get; set; }
        private string SubscriptionName { get { return this.GetType().Name; } }

        public BaseServiceBusWorker()
        {
            Init();
            this.MessageRepostMaxCount = 10;
        }

        protected virtual void Init()
        {
            var namespaceManager = NamespaceManager.CreateFromConnectionString(this.ConnectionString);

            if (!namespaceManager.TopicExists(this.TopicName))
                namespaceManager.CreateTopic(this.TopicName);

            if (!namespaceManager.SubscriptionExists(this.TopicName, this.SubscriptionName))
                namespaceManager.CreateSubscription(this.TopicName, this.SubscriptionName);

            // setup delegates to abstract away Service Bus stuff
            var subClient = SubscriptionClient.CreateFromConnectionString(this.ConnectionString, this.TopicName, this.SubscriptionName);
            this.GetMessage = subClient.Receive;

            var topicClient = TopicClient.CreateFromConnectionString(this.ConnectionString, this.TopicName);
            this.SendMessage = topicClient.Send;
        }

        protected override void Process()
        {
            InfoLogging(string.Format("{0} - Processing", this.SubscriptionName));

            BrokeredMessage message = this.GetMessage();

            if (message != null)
            {
                var messageBody = message.GetBody<string>();
                message.Complete();

                // try to extract a count
                var counts = messageBody.Split('#');
                messageBody = counts[0];
                int messageCount = 0;

                if (counts.Length > 1)
                    Int32.TryParse(counts[1], out messageCount);

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                DebugLogging(string.Format("{0} - Received new message", this.SubscriptionName), message.MessageId);

                try
                {
                    Do(messageBody);
                }
                catch (Exception e)
                {
                    stopWatch.Stop();
                    this.ErrorLogging(string.Format("{0} - Failed to process message.", this.SubscriptionName), message.MessageId, e);

                    if (messageCount < this.MessageRepostMaxCount)
                    {
                        messageCount++;

                        var appendedMessageBody = String.Format("{0}#{1}", messageBody, messageCount);
                        // this means that the message processing has failed and we need to repost the message
                        this.SendMessage(new BrokeredMessage(appendedMessageBody));
                        this.ErrorLogging(string.Format("{0} - Reposting the message, retry #: {1}.", this.SubscriptionName, messageCount), message.MessageId, e);
                    }
                    else
                    {
                        this.ErrorLogging(string.Format("{0} - Done trying to repost message, message has failed to be processed.", this.SubscriptionName));
                    }
                }

                if (stopWatch.IsRunning)
                    stopWatch.Stop();

                TimeSpan timeSpan = stopWatch.Elapsed;
                DebugLogging(string.Format("{0} - Processed message", this.SubscriptionName), message.MessageId, timeSpan.TotalSeconds);
            }
        }
    }
}
