using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;
using Ninject;

namespace Proactima.AzureWorkers
{
    public class ClientFactory : ICreateClients
    {
        private readonly IKernel _kernel;

        public ClientFactory(IKernel kernel)
        {
            _kernel = kernel;
        }

        public async Task<CloudQueue> CreateStorageQueueClientAsync(string queueName)
        {
            var client = _kernel.TryGet<CloudQueue>(queueName);
            if (client != null) return client;

            var queueClient = _kernel.Get<CloudQueueClient>();

            var queue = queueClient.GetQueueReference(queueName);
            await queue.CreateIfNotExistsAsync().ConfigureAwait(false);

            return queue;
        }

        public async Task<QueueClient> CreateServicebusQueueClientAsync(string queueName)
        {
            var client = _kernel.TryGet<QueueClient>(queueName);
            if (client != null) return client;

            var namespaceMgr = _kernel.Get<NamespaceManager>();

            if (!await namespaceMgr.QueueExistsAsync(queueName).ConfigureAwait(false))
                await namespaceMgr.CreateQueueAsync(queueName).ConfigureAwait(false);

            var messagingFactory = _kernel.Get<MessagingFactory>();

            _kernel.Bind<QueueClient>()
                .ToMethod(context => messagingFactory.CreateQueueClient(queueName))
                .InSingletonScope()
                .Named(queueName);

            client = _kernel.Get<QueueClient>(queueName);
            return client;
        }

        public async Task<TopicClient> CreateTopicClientAsync(string topicName)
        {
            var client = _kernel.TryGet<TopicClient>(topicName);
            if (client != null) return client;

            var messagingFactory = _kernel.Get<MessagingFactory>();

            var namespaceMgr = _kernel.Get<NamespaceManager>();
            if (!await namespaceMgr.TopicExistsAsync(topicName).ConfigureAwait(false))
                await namespaceMgr.CreateTopicAsync(topicName).ConfigureAwait(false);

            _kernel.Bind<TopicClient>()
                .ToMethod(context => messagingFactory.CreateTopicClient(topicName))
                .InSingletonScope()
                .Named(topicName);

            client = _kernel.Get<TopicClient>(topicName);
            return client;
        }

        public async Task<SubscriptionClient> CreateSubscriptionClientAsync(string topicName, string subscriptionName)
        {
            var client = _kernel.TryGet<SubscriptionClient>(subscriptionName);
            if (client != null) return client;

            var messagingFactory = _kernel.Get<MessagingFactory>();

            var namespaceMgr = _kernel.Get<NamespaceManager>();
            if (!await namespaceMgr.TopicExistsAsync(topicName).ConfigureAwait(false))
                await namespaceMgr.CreateTopicAsync(topicName).ConfigureAwait(false);

            if (!await namespaceMgr.SubscriptionExistsAsync(topicName, subscriptionName).ConfigureAwait(false))
                await namespaceMgr.CreateSubscriptionAsync(topicName, subscriptionName).ConfigureAwait(false);

            _kernel.Bind<SubscriptionClient>()
                .ToMethod(context => messagingFactory.CreateSubscriptionClient(topicName, subscriptionName))
                .InSingletonScope()
                .Named(subscriptionName);

            client = _kernel.Get<SubscriptionClient>(subscriptionName);
            return client;
        }
    }
}