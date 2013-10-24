using System;
using Ninject.Extensions.Conventions;
using Ninject.Modules;

namespace Proactima.AzureWorkers
{
    internal class AzureWorkerModule : NinjectModule
    {
        public override void Load()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            this.Bind(x => x
                  .From(assemblies)
                  .SelectAllClasses()
                  .InheritedFrom<IWorker>()
                  .BindSingleInterface());

            this.Bind(x => x
                   .From(assemblies)
                   .SelectAllClasses()
                   .InheritedFrom<IStartupTask>()
                   .BindSingleInterface());
        }
    }
}
