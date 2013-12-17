using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
