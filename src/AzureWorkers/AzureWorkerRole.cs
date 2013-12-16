using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.ServiceRuntime;
using Ninject;
using Ninject.Extensions.Azure;
using Ninject.Modules;

namespace Proactima.AzureWorkers
{
    public abstract class AzureWorkerRole : NinjectRoleEntryPoint
    {
        private CancellationTokenSource _cancellationTokenSource;
        private List<Task> Tasks { get; set; }

        protected virtual int TaskTimeout
        {
            get { return 30; }
        }

        [Inject]
        public List<IStartupTask> Starters { get; set; }

        [Inject]
        public List<BaseWorker> Workers { get; set; }

        protected virtual async Task ErrorLogging(string message, Exception ex = null)
        {
            await Task.FromResult(0).ConfigureAwait(false);
        }

        protected virtual async Task InfoLogging(string message)
        {
            await Task.FromResult(0).ConfigureAwait(false);
        }

        protected virtual void AddCustomModules(IList<INinjectModule> moduleList)
        {
        }

        protected abstract void OnRoleStarting();

        protected override IKernel CreateKernel()
        {
            var modules = new List<INinjectModule> {new AzureWorkerModule()};
            AddCustomModules(modules);

            return new StandardKernel(modules.ToArray());
        }

        public override void Run()
        {
            Run(Workers, Starters);
        }

        public async void Run(IEnumerable<BaseWorker> workers, IEnumerable<IStartupTask> starters)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            foreach (var startupItem in starters)
            {
                startupItem.Start();
            }

            Tasks = new List<Task>();
            var enumerable = workers as BaseWorker[] ?? workers.ToArray();
            foreach (var worker in enumerable)
            {
                await worker.OnStart(_cancellationTokenSource.Token).ConfigureAwait(false);
            }

            foreach (var worker in enumerable)
            {
                Tasks.Add(worker.ProtectedRun());
            }

            int completedTaskIndex;
            while ((completedTaskIndex = Task.WaitAny(Tasks.ToArray())) != -1 && Tasks.Count > 0)
            {
                Tasks.RemoveAt(completedTaskIndex);
                if (_cancellationTokenSource.Token.IsCancellationRequested) continue;

                Tasks.Insert(completedTaskIndex, enumerable[completedTaskIndex].ProtectedRun());
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        protected override bool OnRoleStarted()
        {
            ServicePointManager.DefaultConnectionLimit = 12;
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            OnRoleStarting();

            return base.OnRoleStarted();
        }

        protected override void OnRoleStopped()
        {
            _cancellationTokenSource.Cancel();

            foreach (var job in Workers)
            {
                job.OnStop();
                // ReSharper disable once SuspiciousTypeConversion.Global
                var disposable = job as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }

            try
            {
                Task.WaitAll(Tasks.ToArray());
            }
            catch (AggregateException)
            {
            }

            base.OnRoleStopped();
        }

        protected virtual void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
                e.Cancel = true;
        }
    }
}