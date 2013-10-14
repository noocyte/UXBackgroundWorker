using Ninject.Extensions.Conventions;
using Ninject.Modules;
using System;
using System.Linq;
using System.Collections;

namespace UXBackgroundWorker
{
    internal class UXBackgroundWorkerModule : NinjectModule
    {
        public override void Load()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList()
                .Where(a => !a.FullName.Contains("Anonymously Hosted DynamicMethods Assembly"));


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
