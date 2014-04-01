using FluentAssertions;
using Ninject;
using Ninject.Extensions.Conventions;
using Ninject.Extensions.Conventions.BindingGenerators;
using Ninject.Syntax;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureWorkers.Tests
{
    public abstract class Foo
    {
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InstanceAttribute : Attribute
    {
        public InstanceAttribute(int instanceCount)
        {
            this.InstanceCount = instanceCount;
        }
        public int InstanceCount { get; private set; }
    }

    [Instance(2)]
    public class SomeFoo : Foo
    {
    }

    [Instance(2)]
    public class OtherFoo : Foo
    {
    }

    public class Bar : Foo
    {
    }

    public class SomeFooUser
    {
        public IEnumerable<Foo> Foos { get; set; }

        public SomeFooUser(IEnumerable<Foo> foos)
        {
            this.Foos = foos;
        }
    }

    public class MultiBaseBindingGenerator : IBindingGenerator
    {
        public IEnumerable<IBindingWhenInNamedWithOrOnSyntax<object>> CreateBindings(Type type, IBindingRoot bindingRoot)
        {
            int count = 1;
            var countAttribute = (InstanceAttribute)Attribute.GetCustomAttribute(type, typeof(InstanceAttribute));
            if (countAttribute != null)
                count = countAttribute.InstanceCount;

            for (int i = 0; i < count; i++)
            {
                yield return bindingRoot.Bind(type.BaseType).To(type);
            }
        }
    }

    [TestFixture]
    public class Demo
    {
        [Test]
        public void FactMethodName()
        {
            var kernel = new StandardKernel();

            kernel.Bind(x => x.FromThisAssembly()
                .SelectAllClasses()
                .InheritedFrom<Foo>()
                .BindWith<MultiBaseBindingGenerator>());

            kernel.Get<SomeFooUser>().Foos.Should().HaveCount(5);
        }
    }

}
