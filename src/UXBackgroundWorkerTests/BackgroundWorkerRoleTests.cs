using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UXBackgroundWorker;

namespace UXBackgroundWorkerTests
{
    [TestClass]
    public class BackgroundWorkerRoleTests
    {
        [TestMethod]
        public void RunsStartupTasks()
        {
            var testee = new TestableWorker();
            testee.Run();

            Assert.IsTrue(SimpleStarter.HasBeenCalled);
        }

        class TestableWorker : BackgroundWorkerRole
        {
        }

        class SimpleStarter : IStartupTask
        {
            public static bool HasBeenCalled { get; private set; }

            public void Start()
            {
                HasBeenCalled = true;
            }
        }

    }
}
