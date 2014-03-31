using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using RetryPolicy = Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling.RetryPolicy;

namespace Proactima.AzureWorkers
{
    public abstract class BaseServiceBusTopicWorker : BaseServiceBusWorker
    {
        private RetryPolicy _retryStrategy;

        protected BaseServiceBusTopicWorker()
        {
            MessageRepostMaxCount = 3;
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
            InfoLogging(string.Format("{0} - Processing", SubscriptionName));

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
            _retryStrategy = CreateRetryPolicy(MessageRepostMaxCount);

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
                var failed = false;

                try
                {
                    try
                    {
                        await _retryStrategy.ExecuteAsync(() =>
                            Do(messageBody), Token).ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        ErrorLogging("Message processing failed after retry, posting as failed message.", exception);
                        failed = true;
                    }
                }
                finally
                {
                    if (stopWatch.IsRunning)
                        stopWatch.Stop();

                    var timeSpan = stopWatch.Elapsed;
                    DebugLogging(string.Format("{0} - Processed message", SubscriptionName), message.MessageId,
                        timeSpan.TotalSeconds);
                }

                if (failed)
                    await HandleFailedMessageAsync(messageBody).ConfigureAwait(false);
            }
        }

        protected abstract Task HandleFailedMessageAsync(string messageBody);

        private static RetryPolicy CreateRetryPolicy(int count)
        {
            var strategy = new TopicTransientErrorDetectionStrategy();
            return Exponential(strategy, count, 30, 0.5);
        }

        private static RetryPolicy Exponential(
            ITransientErrorDetectionStrategy strategy,
            int retryCount = 3,
            double maxBackoffDelayInSeconds = 1024,
            double delta = 2)
        {
            var exponentialBackoff = new ExponentialBackoff(retryCount,
                TimeSpan.FromSeconds(0),
                TimeSpan.FromSeconds(maxBackoffDelayInSeconds),
                TimeSpan.FromSeconds(delta));

            return new RetryPolicy(strategy,
                exponentialBackoff);
        }
    }
}