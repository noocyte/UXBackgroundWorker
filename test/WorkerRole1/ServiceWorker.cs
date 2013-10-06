using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UXBackgroundWorker;

namespace WorkerRole1
{
    public class ServiceWorker : BaseServiceBusWorker
    {
        protected override string ConnectionString
        {
            get { return ""; }
        }

        protected override void Do(string message)
        {
            InfoLogging("Got a message!");
        }

        protected override string TopicName
        {
            get { return "ServiceWorker"; }
        }
    }
}
