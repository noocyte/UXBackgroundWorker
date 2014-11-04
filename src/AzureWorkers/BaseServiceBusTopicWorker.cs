using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Proactima.AzureWorkers
{
    public abstract class BaseServiceBusTopicWorker : BaseServiceBusWorker
    {
        private readonly ICreateClients _clientFactory;
        private RetryPolicy _retryStrategy;

        protected BaseServiceBusTopicWorker(ICreateClients clientFactory)
        {
            _clientFactory = clientFactory;
            MessageRepostMaxCount = 3;
        }

        protected abstract string TopicName { get; }

        protected int MessageRepostMaxCount { get; set; }

        private string SubscriptionName
        {
            get { return GetType().Name; }
        }

        protected abstract Task Do(string message);

        protected override async Task StartAsync()
        {
            InfoLogging(string.Format("{0} - Processing", SubscriptionName));

            var subClient = _clientFactory.CreateSubscriptionClient(TopicName, SubscriptionName);

            _retryStrategy = CreateRetryPolicy(MessageRepostMaxCount);
            var stopWatch = new Stopwatch();

            while (!Token.IsCancellationRequested)
            {
                var message = await subClient.ReceiveAsync(new TimeSpan(0, 0, 10)).ConfigureAwait(false);
                if (message == null) continue;

                var messageBody = message.GetBody<string>();
                if (String.IsNullOrEmpty(messageBody))
                    messageBody = String.Empty;

                var messageId = message.MessageId;
                message.Complete();

                DebugLogging(string.Format("{0} - Received new message", SubscriptionName), messageId);
                var failed = false;
                stopWatch.Restart();

                try
                {
                    await _retryStrategy.ExecuteAsync(() =>
                        Do(messageBody), Token).ConfigureAwait(false);

                    stopWatch.Stop();
                    var timeSpan = stopWatch.Elapsed;
                    DebugLogging(string.Format("{0} - Processed message",
                        SubscriptionName), messageId,
                        timeSpan.TotalSeconds);
                }
                catch (Exception exception)
                {
                    ErrorLogging("Message processing failed after retry, posting as failed message.", messageId,
                        exception);
                    failed = true;
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