using System.Collections.Generic;
using Proactima.AzureWorkers;

namespace AzureWorkers.Tests
{
    public class SomeFooUser
    {
        public SomeFooUser(IEnumerable<Foo> foos)
        {
            Foos = foos;
        }

        public IEnumerable<Foo> Foos { get; set; }
    }

    public abstract class Foo
    {
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
}