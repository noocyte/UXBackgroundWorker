using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Ninject;

namespace Proactima.AzureWorkers
{
    public interface ICreateClients
    {
        TopicClient CreateTopicClient(string topicName);
        SubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName);
    }

    public class ClientFactory : ICreateClients
    {
        private readonly IKernel _kernel;

        public ClientFactory(IKernel kernel)
        {
            _kernel = kernel;
        }

        public TopicClient CreateTopicClient(string topicName)
        {
            var client = _kernel.TryGet<TopicClient>(topicName);
            if (client != null) return client;

            var messagingFactory = _kernel.Get<MessagingFactory>();

            var namespaceMgr = _kernel.Get<NamespaceManager>();
            if (!namespaceMgr.TopicExists(topicName))
                namespaceMgr.CreateTopic(topicName);

            _kernel.Bind<TopicClient>()
                .ToMethod(context => messagingFactory.CreateTopicClient(topicName))
                .InSingletonScope()
                .Named(topicName);

            client = _kernel.Get<TopicClient>(topicName);
            return client;
        }

        public SubscriptionClient CreateSubscriptionClient(string topicName, string subscriptionName)
        {
            var client = _kernel.TryGet<SubscriptionClient>(subscriptionName);
            if (client != null) return client;

            var messagingFactory = _kernel.Get<MessagingFactory>();

            var namespaceMgr = _kernel.Get<NamespaceManager>();
            if (!namespaceMgr.TopicExists(topicName))
                namespaceMgr.CreateTopic(topicName);

            if (!namespaceMgr.SubscriptionExists(topicName, subscriptionName))
                namespaceMgr.CreateSubscription(topicName, subscriptionName);

            _kernel.Bind<SubscriptionClient>()
                .ToMethod(context => messagingFactory.CreateSubscriptionClient(topicName, subscriptionName))
                .InSingletonScope()
                .Named(subscriptionName);

            client = _kernel.Get<SubscriptionClient>(subscriptionName);
            return client;
        }
    }
}