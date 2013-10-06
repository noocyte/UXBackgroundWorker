using Ninject.Extensions.Conventions;
using Microsoft.WindowsAzure.ServiceRuntime;
using Ninject;
using Ninject.Extensions.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace UXBackgroundWorker
{
    public abstract class BackgroundWorkerRole : NinjectRoleEntryPoint
    {
        private bool KeepRunning;
        protected IEnumerable<IWorker> Processors { get; set; }
        protected List<Task> Tasks { get; set; }

        protected IKernel Kernel;

        protected abstract void ErrorLogging(string message, Exception e = null);
        protected abstract void InfoLogging(string message);
        protected Assembly Assembly { get { return System.Reflection.Assembly.GetExecutingAssembly(); } }

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
            foreach (var startup in startupTasks)
                startup.Start();
            
            
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
                        // Observe unhandled exception
                        if (task.Exception != null)
                        {
                            this.ErrorLogging("Job threw an exception", task.Exception.InnerException);
                        }
                        else
                        {
                            this.ErrorLogging("Job failed with no exception");
                        }

                        var jobToRestart = this.Processors.ElementAt(i);
                        this.Tasks[i] = Task.Factory.StartNew(jobToRestart.Start);
                    }
                }

                Thread.Sleep(TimeSpan.FromSeconds(30));
            }

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