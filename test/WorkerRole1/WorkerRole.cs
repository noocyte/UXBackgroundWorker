using Ninject.Extensions.Conventions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Diagnostics;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.Storage;
using UXBackgroundWorker;
using System.Reflection;

namespace WorkerRole1
{
    public class WorkerRole : BackgroundWorkerRole
    {
    }
}
