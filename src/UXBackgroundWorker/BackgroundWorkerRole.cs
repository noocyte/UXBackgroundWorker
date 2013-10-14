using Microsoft.WindowsAzure.ServiceRuntime;
using Ninject;
using Ninject.Extensions.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace UXBackgroundWorker
{
    public abstract class BackgroundWorkerRole : NinjectRoleEntryPoint
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ManualResetEvent _safeToExitHandle;

        private List<Task> Tasks { get; set; }

        protected virtual void ErrorLogging(string message, Exception ex = null) { }
        protected virtual void InfoLogging(string message) { }
        protected virtual int TaskTimeout { get { return 30; } }

        [Inject]
        public IEnumerable<IStartupTask> Starters { get; set; }
        [Inject]
        public IEnumerable<BaseWorker> Workers { get; set; }

        protected override IKernel CreateKernel()
        {
            var kernel = new StandardKernel(new UXBackgroundWorkerModule());
            return kernel;
        }

        public override void Run()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _safeToExitHandle = new ManualResetEvent(false);
            var token = _cancellationTokenSource.Token;

            // handle any startup tasks first
            foreach (var startupItem in this.Starters)
            {
                startupItem.Start();
            }

            // now handle the actual workers
            this.Tasks = new List<Task>();

            foreach (var worker in this.Workers)
            {
                var t = Task.Factory.StartNew(worker.Start);
                this.Tasks.Add(t);
            }

            // Control and restart a faulted job
            while (!token.IsCancellationRequested)
            {
                for (int i = 0; i < this.Tasks.Count; i++)
                {
                    var task = this.Tasks[i];
                    if (task.IsFaulted)
                    {
                        LogUnhandledException(task);
                        var jobToRestart = this.Workers.ElementAt(i);
                        this.Tasks[i] = Task.Factory.StartNew(jobToRestart.Start);
                    }
                }

                token.WaitHandle.WaitOne(this.TaskTimeout * 1000);
            }

            _safeToExitHandle.Set();
        }

        private void LogUnhandledException(Task task)
        {
            // Observe unhandled exception
            if (task.Exception != null)
                this.ErrorLogging("Job threw an exception", task.Exception.InnerException);
            else
                this.ErrorLogging("Job failed with no exception");
        }

        protected override bool OnRoleStarted()
        {
            ServicePointManager.DefaultConnectionLimit = 12;
            RoleEnvironment.Changing += this.RoleEnvironmentChanging;

            return base.OnRoleStarted();
        }

        protected override void OnRoleStopped()
        {
            _cancellationTokenSource.Cancel();

            foreach (var job in this.Workers)
            {
                job.Stop();
                var disposable = job as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }

            try
            {
                Task.WaitAll(this.Tasks.ToArray());
            }
            catch (AggregateException ex)
            {
                // Observe any unhandled exceptions.
                this.ErrorLogging(String.Format("Finalizing exception thrown: {0} exceptions", ex.InnerExceptions.Count), ex);
            }

            _safeToExitHandle.WaitOne();

            this.InfoLogging("Worker is stopped");

            base.OnRoleStopped();
        }

        protected virtual void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
                e.Cancel = true;
        }
    }
}