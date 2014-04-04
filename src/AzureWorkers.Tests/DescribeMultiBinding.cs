using System.Linq;
using FluentAssertions;
using Ninject;
using Ninject.Extensions.Conventions;
using NUnit.Framework;
using Proactima.AzureWorkers;

namespace AzureWorkers.Tests
{
    [TestFixture]
    public class DescribeMultiBinding
    {
        private static StandardKernel CreateKernel()
        {
            var kernel = new StandardKernel();

            kernel.Bind(x => x.FromThisAssembly()
                .SelectAllClasses()
                .InheritedFrom<Foo>()
                .BindWith<MultiBaseBindingGenerator>());
            return kernel;
        }

        [Test]
        public void ItShouldCreateMultipleInstances()
        {
            // g
            var kernel = CreateKernel();

            // w
            var foos = kernel.Get<SomeFooUser>().Foos.ToList();

            // t
            foos.Should().HaveCount(5);
            foos.Count(f => f.GetType() == typeof (SomeFoo)).Should().Be(2);
            foos.Count(f => f.GetType() == typeof (OtherFoo)).Should().Be(2);
        }

        [Test]
        public void ItShouldCreateOneInstance_GivenNoAttribute()
        {
            // g
            var kernel = CreateKernel();

            // w
            var foos = kernel.Get<SomeFooUser>().Foos.ToList();

            // t
            foos.Count(f => f.GetType() == typeof (Bar)).Should().Be(1);
        }
    }
}