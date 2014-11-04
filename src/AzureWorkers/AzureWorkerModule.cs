using System;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Ninject;
using Ninject.Extensions.Conventions;
using Ninject.Modules;

namespace Proactima.AzureWorkers
{
    internal class AzureWorkerModule : NinjectModule
    {
        private readonly string _servicebusConnection =
            CloudConfigurationManager.GetSetting("ServiceBusConnectionString");
        private readonly string _storageConnection =
    CloudConfigurationManager.GetSetting("StorageConnectionString");

        public override void Load()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Bind<ICreateClients>().To<ClientFactory>();
            Bind<CloudStorageAccount>().ToMethod(context =>
            {
                var storageAccount = CloudStorageAccount.Parse(_storageConnection);
                return storageAccount;
            }).InSingletonScope();

            Bind<CloudQueueClient>().ToMethod(context =>
            {
                var storageAccount = Kernel.Get<CloudStorageAccount>();
                var queueClient = storageAccount.CreateCloudQueueClient();
                return queueClient;
            }).InSingletonScope();
            
            Bind<NamespaceManager>()
                .ToMethod(context =>
                {
                    var namespaceManager = NamespaceManager.CreateFromConnectionString(_servicebusConnection);
                    return namespaceManager;
                })
                .InSingletonScope();

            Bind<MessagingFactory>()
                .ToMethod(context =>
                {
                    var fac = MessagingFactory.CreateFromConnectionString(_servicebusConnection);
                    return fac;
                })
                .InSingletonScope();

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