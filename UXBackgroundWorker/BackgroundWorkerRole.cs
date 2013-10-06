using Microsoft.WindowsAzure.ServiceRuntime;
using Ninject;
using Ninject.Extensions.Azure;
using Ninject.Extensions.Conventions;
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
        private bool KeepRunning;
        private IEnumerable<IWorker> Processors { get; set; }
        private List<Task> Tasks { get; set; }

        protected IKernel Kernel;
        protected virtual void ErrorLogging(string message, Exception e = null)
        { }
        protected virtual void InfoLogging(string message)
        { }

        protected virtual int TaskTimeout { get { return 30; } }

        protected override IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            this.Kernel = kernel;

            kernel.Bind(x => x
                   .From(AppDomain.CurrentDomain.GetAssemblies())
                   .SelectAllClasses()
                   .InheritedFrom<IWorker>()
                   .BindSingleInterface());

            return kernel;
        }

        public override void Run()
        {
            this.KeepRunning = true;

            // handle any startup tasks first
            var startupTasks = this.Kernel.GetAll<IStartupTask>().ToList();
            startupTasks.ForEach(s => s.Start());

            // now handle the actual workers
            this.Processors = this.Kernel.GetAll<IWorker>().ToList();
            this.Tasks = new List<Task>();

            foreach (var processor in this.Processors)
            {
                var t = Task.Factory.StartNew(processor.Start);
                this.Tasks.Add(t);
            }

            // Control and restart a faulted job
            while (this.KeepRunning)
            {
                for (int i = 0; i < this.Tasks.Count; i++)
                {
                    var task = this.Tasks[i];
                    if (task.IsFaulted)
                    {
                        LogUnhandledException(task);
                        var jobToRestart = this.Processors.ElementAt(i);
                        this.Tasks[i] = Task.Factory.StartNew(jobToRestart.Start);
                    }
                }

                Thread.Sleep(TimeSpan.FromSeconds(this.TaskTimeout));
            }
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
            this.KeepRunning = false;

            foreach (var job in this.Processors)
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
                this.ErrorLogging(String.Format("Finalizing exception thrown: {0} exceptions", ex.InnerExceptions.Count));
            }

            this.InfoLogging("Worker is stopped");

            base.OnRoleStopped();
        }

        private void RoleEnvironmentChanging(object sender, RoleEnvironmentChangingEventArgs e)
        {
            if (e.Changes.Any(change => change is RoleEnvironmentConfigurationSettingChange))
                e.Cancel = true;
        }
    }
}