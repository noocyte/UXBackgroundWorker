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
        private ManualResetEvent _safeToExitHandle;
        private List<Task> Tasks { get; set; }

        protected virtual int TaskTimeout
        {
            get { return 30; }
        }

        [Inject]
        public List<IStartupTask> Starters { get; set; }

        [Inject]
        public List<IWorker> Workers { get; set; }

        protected virtual void ErrorLogging(string message, Exception ex = null)
        {
        }

        protected virtual void InfoLogging(string message)
        {
        }

        protected virtual void AddCustomModules(IList<INinjectModule> moduleList)
        {
        }

        protected override IKernel CreateKernel()
        {
            var modules = new List<INinjectModule> {new AzureWorkerModule()};
            AddCustomModules(modules);

            return new StandardKernel(modules.ToArray());
        }

        public override void Run()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _safeToExitHandle = new ManualResetEvent(false);
            var token = _cancellationTokenSource.Token;

            foreach (var startupItem in Starters)
            {
                startupItem.Start();
            }

            Tasks = new List<Task>();
            foreach (var worker in Workers)
            {
                var t = Task.Factory.StartNew(worker.Start, token);
                Tasks.Add(t);
            }

            // Control and restart a faulted job
            while (!token.IsCancellationRequested)
            {
                for (var i = 0; i < Tasks.Count; i++)
                {
                    var task = Tasks[i];
                    if (!task.IsFaulted) continue;

                    LogUnhandledException(task);
                    var jobToRestart = Workers.ElementAt(i);
                    Tasks[i] = Task.Factory.StartNew(jobToRestart.Start, token);
                }

                token.WaitHandle.WaitOne(TaskTimeout*1000);
            }

            _safeToExitHandle.Set();
        }

        private void LogUnhandledException(Task task)
        {
            // Observe unhandled exception
            if (task.Exception != null)
                ErrorLogging("Job threw an exception", task.Exception.InnerException);
            else
                ErrorLogging("Job failed with no exception");
        }

        protected override bool OnRoleStarted()
        {
            ServicePointManager.DefaultConnectionLimit = 12;
            RoleEnvironment.Changing += RoleEnvironmentChanging;

            return base.OnRoleStarted();
        }

        protected override void OnRoleStopped()
        {
            _cancellationTokenSource.Cancel();

            foreach (var job in Workers)
            {
                job.Stop();
                // ReSharper disable once SuspiciousTypeConversion.Global
                var disposable = job as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }

            try
            {
                Task.WaitAll(Tasks.ToArray());
            }
            catch (AggregateException ex)
            {
                // Observe any unhandled exceptions.
                ErrorLogging(String.Format("Finalizing exception thrown: {0} exceptions", ex.InnerExceptions.Count), ex);
            }

            _safeToExitHandle.WaitOne();

            InfoLogging("Worker is stopped");

            base.OnRoleStopped();
        }

        protected virtual void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
                e.Cancel = true;
        }
    }
}