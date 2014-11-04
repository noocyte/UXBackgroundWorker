using System;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Ninject.Extensions.Conventions;
using Ninject.Modules;

namespace Proactima.AzureWorkers
{
    internal class AzureWorkerModule : NinjectModule
    {
        private readonly string _servicebusConnection =
            CloudConfigurationManager.GetSetting("ServiceBusConnectionString");

        public override void Load()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            Bind<ICreateClients>().To<ClientFactory>();
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