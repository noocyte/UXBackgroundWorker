using Ninject.Extensions.Conventions;
using Ninject.Modules;
using System;

namespace UXBackgroundWorker
{
    internal class UXBackgroundWorkerModule : NinjectModule
    {
        public override void Load()
        {
            this.Bind(x => x
                  .From(AppDomain.CurrentDomain.GetAssemblies())
                  .SelectAllClasses()
                  .InheritedFrom<BaseWorker>()
                  .BindSingleInterface());

            this.Bind(x => x
                   .From(AppDomain.CurrentDomain.GetAssemblies())
                   .SelectAllClasses()
                   .InheritedFrom<IStartupTask>()
                   .BindSingleInterface());
        }
    }
}
