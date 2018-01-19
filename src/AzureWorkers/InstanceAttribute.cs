using System;

namespace Proactima.AzureWorkers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InstanceAttribute : Attribute
    {
        public InstanceAttribute(int instanceCount)
        {
            InstanceCount = instanceCount;
        }
        public int InstanceCount { get; private set; }
    }
}