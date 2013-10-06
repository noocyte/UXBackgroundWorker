using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using System;
using System.Diagnostics;

namespace UXBackgroundWorker
{
    public abstract class BaseServiceBusWorker : BaseWorker
    {
        protected abstract void Do(string message);
        protected abstract string TopicName { get; }
        protected abstract void ErrorLogging(string message, string messageId = "", Exception e = null);
        protected abstract void InfoLogging(string message, string messageId = "");
        protected abstract void DebugLogging(string message, string messageId = "", double timerValue = 0.0);
        protected abstract string ConnectionString { get; }

        private string SubscriptionName { get { return this.GetType().Name; } }
        
        public BaseServiceBusWorker()
        {
            Init();
        }

        protected virtual void Init()
        {
            //this.ConnectionString = CloudConfigurationManager.GetSetting("TopicConnectionString");

            var namespaceManager = NamespaceManager.CreateFromConnectionString(this.ConnectionString);

            if (!namespaceManager.TopicExists(this.TopicName))
                namespaceManager.CreateTopic(this.TopicName);

            if (!namespaceManager.SubscriptionExists(this.TopicName, this.SubscriptionName))
                namespaceManager.CreateSubscription(this.TopicName, this.SubscriptionName);
        }

        protected override void Process()
        {
            InfoLogging(string.Format("{0} Processing", this.SubscriptionName));

            var subClient = SubscriptionClient.CreateFromConnectionString(this.ConnectionString, this.TopicName, this.SubscriptionName);

            subClient.Receive();

            while (this.KeepRunning)
            {
                BrokeredMessage message = subClient.Receive();

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
                    DebugLogging(string.Format("{0} Recieved new message", this.SubscriptionName), message.MessageId);

                    try
                    {
                        Do(messageBody);
                    }
                    catch (Exception e)
                    {
                        stopWatch.Stop();
                        this.ErrorLogging(string.Format("{0} Failed to process message.", this.SubscriptionName), message.MessageId, e);

                        if (messageCount < 10)
                        {
                            messageCount++;

                            var appendedMessageBody = String.Format("{0}#{1}", messageBody, messageCount);
                            // this means that the message processing has failed and we need to repost the message
                            var topicClient = TopicClient.CreateFromConnectionString(this.ConnectionString, this.TopicName);
                            topicClient.Send(new BrokeredMessage(appendedMessageBody));

                            this.ErrorLogging(string.Format("{0} Reposting the message, retry #: {1}.", this.SubscriptionName, messageCount), message.MessageId, e);
                        }
                    }

                    if (stopWatch.IsRunning)
                        stopWatch.Stop();

                    TimeSpan timeSpan = stopWatch.Elapsed;
                    DebugLogging(string.Format("{0} Processed message", this.SubscriptionName), message.MessageId, timeSpan.TotalSeconds);
                }
            }
        }
    }
}
