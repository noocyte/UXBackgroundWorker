using Microsoft.WindowsAzure;

namespace Proactima.AzureWorkers
{
    public abstract class BaseServiceBusWorker : BaseWorker
    {
        protected virtual string ConnectionString
        {
            get { return CloudConfigurationManager.GetSetting("ServiceBusConnectionString"); }
        }

        protected override int LoopWaitTime
        {
            get { return 0; }
        }
    }
}