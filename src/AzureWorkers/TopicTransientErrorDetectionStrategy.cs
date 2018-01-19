using System;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Proactima.AzureWorkers
{
    public class TopicTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        public bool IsTransient(Exception ex)
        {
            return true;
        }
    }
}