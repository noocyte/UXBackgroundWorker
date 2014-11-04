﻿using System;
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
        private IKernel _kernel;
        private List<Task> Tasks { get; set; }

        protected virtual int TaskTimeout
        {
            get { return 30; }
        }

        [Inject]
        public List<IStartupTask> Starters { get; set; }

        [Inject]
        public List<BaseWorker> Workers { get; set; }

        protected virtual void ErrorLogging(string message, Exception ex = null)
        {
        }

        protected virtual void InfoLogging(string message)
        {
        }

        protected virtual void AddCustomModules(IList<INinjectModule> moduleList)
        {
        }

        protected virtual void OnRoleIsStarting()
        {
        }

        protected virtual void OnRoleIsStopping()
        {
        }

        protected override IKernel CreateKernel()
        {
            var modules = new List<INinjectModule> {new AzureWorkerModule()};
            AddCustomModules(modules);
            var ninjectModules = modules.ToArray();

            InfoLogging(String.Format("Adding {0} Ninject modules to the kernel", ninjectModules.Length));

            _kernel = new StandardKernel(ninjectModules);
            return _kernel;
        }

        public override void Run()
        {
            Run(Workers, Starters);
        }

        private async void Run(IEnumerable<BaseWorker> workers, IEnumerable<IStartupTask> starters)
        {
            _cancellationTokenSource = new CancellationTokenSource();

            InfoLogging("About to run startup tasks...");

            foreach (var startupItem in starters)
            {
                startupItem.Start();
            }

            InfoLogging("Finished running startup tasks...");

            Tasks = new List<Task>();
            var enabledWorkers = workers.Where(w => w.Enabled).ToList();

            InfoLogging(String.Format("About to run {0} enabled workers...", enabledWorkers.Count));

            foreach (var worker in enabledWorkers)
            {
                Tasks.Add(worker.ProtectedRun(_cancellationTokenSource));
            }

            InfoLogging(String.Format("Finished running {0} enabled workers...", enabledWorkers.Count));

            int completedTaskIndex;
            while ((completedTaskIndex = Task.WaitAny(Tasks.ToArray())) != -1 && Tasks.Count > 0)
            {
                Tasks.RemoveAt(completedTaskIndex);
                if (_cancellationTokenSource.Token.IsCancellationRequested) continue;

                Tasks.Insert(completedTaskIndex,
                    enabledWorkers[completedTaskIndex].ProtectedRun(_cancellationTokenSource));
                await Task.Delay(1000).ConfigureAwait(false);
            }
        }

        protected override bool OnRoleStarted()
        {
            InfoLogging("About to startup role...");

            ServicePointManager.DefaultConnectionLimit = 12;
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            OnRoleIsStarting();
            InfoLogging("Finished role startup...");
            return base.OnRoleStarted();
        }

        protected override void OnRoleStopped()
        {
            InfoLogging("About to shutdown role...");

            _cancellationTokenSource.Cancel();

            foreach (var worker in Workers)
            {
                worker.OnStop();
                // ReSharper disable once SuspiciousTypeConversion.Global
                var disposable = worker as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }

            try
            {
                Task.WaitAll(Tasks.ToArray());
            }
            catch (AggregateException exception)
            {
                ErrorLogging("An aggregate exception was caught...", exception);
            }

            OnRoleIsStopping();

            InfoLogging("Finished role shutdown...");
            base.OnRoleStopped();
        }

        protected virtual void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            InfoLogging("RoleEnvironmentChanging was called...");
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
                e.Cancel = true;
        }
    }
}