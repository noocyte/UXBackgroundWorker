using System;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Ninject;
using Ninject.Extensions.Conventions;
using Ninject.Modules;

namespace Proactima.AzureWorkers
{
    internal class AzureWorkerModule : NinjectModule
    {
        public override void Load()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Bind<NamespaceManager>()
                .ToMethod(context =>
                {
                    var cs = CloudConfigurationManager.GetSetting("ServiceBusConnectionString");
                    var namespaceManager = NamespaceManager.CreateFromConnectionString(cs);
                    return namespaceManager;
                })
                .InSingletonScope();

            Bind<MessagingFactory>().ToMethod(context =>
            {
                var cs = CloudConfigurationManager.GetSetting("ServiceBusConnectionString");
                var fac = MessagingFactory.CreateFromConnectionString(cs);
                return fac;
            }).InSingletonScope();

            // TopicClient
            var topicClientFunc = new Func<string, TopicClient>(topicName =>
            {
                var client = Kernel.TryGet<TopicClient>(topicName);
                if (client != null) return client;

                var messagingFactory = Kernel.Get<MessagingFactory>();

                var namespaceMgr = Kernel.Get<NamespaceManager>();
                if (!namespaceMgr.TopicExists(topicName))
                    namespaceMgr.CreateTopic(topicName);

                Kernel.Bind<TopicClient>()
                    .ToMethod(context => messagingFactory.CreateTopicClient(topicName))
                    .InSingletonScope()
                    .Named(topicName);

                client = Kernel.Get<TopicClient>(topicName);
                return client;
            });

            Bind<Func<string, TopicClient>>()
                .ToConstant(topicClientFunc);

            // SubClient
            var subClientFunc = new Func<string, string, SubscriptionClient>((topicName, subName) =>
            {
                var messagingFactory = Kernel.Get<MessagingFactory>();
                var client = Kernel.TryGet<SubscriptionClient>(subName);

                if (client != null) return client;

                var namespaceMgr = Kernel.Get<NamespaceManager>();
                if (!namespaceMgr.TopicExists(topicName))
                    namespaceMgr.CreateTopic(topicName);

                if (!namespaceMgr.SubscriptionExists(topicName, subName))
                    namespaceMgr.CreateSubscription(topicName, subName);


                Kernel.Bind<SubscriptionClient>()
                    .ToMethod(context => messagingFactory.CreateSubscriptionClient(topicName, subName))
                    .InSingletonScope()
                    .Named(subName);


                client = Kernel.Get<SubscriptionClient>(subName);
                return client;
            });

            Bind<Func<string, string, SubscriptionClient>>()
                .ToConstant(subClientFunc);

            this.Bind(x => x
                .From(assemblies)
                .SelectAllClasses()
                .InheritedFrom<BaseWorker>()
                .BindWith<MultiBaseBindingGenerator>());

            this.Bind(x => x
                .From(assemblies)
                .SelectAllClasses()
                .InheritedFrom<IStartupTask>()
                .BindSingleInterface());
        }
    }
}