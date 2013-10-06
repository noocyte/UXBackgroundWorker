using Ninject.Extensions.UXRiskLogger;

namespace UXBackgroundWorker
{
    public abstract class BaseWorker : IWorker
    {
        public ILogger Logger { get; set; }
        protected bool KeepRunning;

        public BaseWorker() { }

        protected abstract void Process();

        public void Start()
        {
            this.KeepRunning = true;
            while (this.KeepRunning)
            {
                this.Process();
            }
        }

        public void Stop()
        {
            this.KeepRunning = false;
        }
    }
}
