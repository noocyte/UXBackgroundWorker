using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UXBackgroundWorker;

namespace WorkerRole1
{
    public class SimpleWorker : BaseWorker
    {

        protected override void Process()
        {
            Thread.Sleep(1000);
        }
    }
}
