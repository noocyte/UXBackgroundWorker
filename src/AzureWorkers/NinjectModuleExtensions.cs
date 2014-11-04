using System;
using System.Linq;
using Ninject.Activation;
using Ninject.Modules;

namespace Proactima.AzureWorkers
{
    internal static class NinjectModuleExtensions
    {
        public static void BindUnlessBoundAsSingleton<T>(this NinjectModule root,
            Func<IContext, T> toMethod)
        {
            if (!root.Kernel.GetBindings(typeof (T)).Any())
                root.Bind<T>().ToMethod(toMethod).InSingletonScope();
        }
    }
}