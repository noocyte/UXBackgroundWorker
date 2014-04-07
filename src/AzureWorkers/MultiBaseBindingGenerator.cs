using System;
using System.Collections.Generic;
using System.Linq;
using Ninject.Extensions.Conventions.BindingGenerators;
using Ninject.Syntax;

namespace Proactima.AzureWorkers
{
    public class MultiBaseBindingGenerator : IBindingGenerator
    {
        public IEnumerable<IBindingWhenInNamedWithOrOnSyntax<object>> CreateBindings(Type type, IBindingRoot bindingRoot)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                return Enumerable.Empty<IBindingWhenInNamedWithOrOnSyntax<object>>();
            }

            var bindings = RecursivelyBindToBaseTypes(type, bindingRoot);
            return bindings;
        }

        private static IEnumerable<IBindingWhenInNamedWithOrOnSyntax<object>> BindWithAttribute(Type type, Type baseType, IBindingRoot bindingRoot)
        {
            var bindings = new List<IBindingWhenInNamedWithOrOnSyntax<object>>();

            var count = 1;
            var countAttribute = (InstanceAttribute) Attribute.GetCustomAttribute(type, typeof (InstanceAttribute));
            if (countAttribute != null)
                count = countAttribute.InstanceCount;

            for (var i = 0; i < count; i++)
            {
                bindings.Add(bindingRoot.Bind(baseType).To(type));
            }

            return bindings;
        }

        private static IEnumerable<IBindingWhenInNamedWithOrOnSyntax<object>> RecursivelyBindToBaseTypes(Type type,
            IBindingRoot bindingRoot)
        {
            var baseType = type.BaseType;
            var bindings = new List<IBindingWhenInNamedWithOrOnSyntax<object>>();
            if (baseType == null) return bindings;

            bindings.AddRange(BindWithAttribute(type, type.BaseType, bindingRoot));
            if (!ShouldBeBound(baseType)) return bindings;


            var ancestor = baseType.BaseType;
            while (ancestor != null && ShouldBeBound(ancestor))
            {
                bindings.AddRange(BindWithAttribute(type, ancestor, bindingRoot));
                ancestor = ancestor.BaseType;
            }
            return bindings;
        }

        private static bool ShouldBeBound(Type type)
        {
            return type.IsClass && type != typeof (object);
        }
    }
}