using System;
using System.Collections.Generic;
using Ninject.Extensions.Conventions.BindingGenerators;
using Ninject.Syntax;

namespace Proactima.AzureWorkers
{
    public class MultiBaseBindingGenerator : IBindingGenerator
    {
        public IEnumerable<IBindingWhenInNamedWithOrOnSyntax<object>> CreateBindings(Type type, IBindingRoot bindingRoot)
        {
            var count = 1;
            var countAttribute = (InstanceAttribute)Attribute.GetCustomAttribute(type, typeof(InstanceAttribute));
            if (countAttribute != null)
                count = countAttribute.InstanceCount;

            for (var i = 0; i < count; i++)
            {
                yield return bindingRoot.Bind(type.BaseType).To(type);
            }
        }
    }
}