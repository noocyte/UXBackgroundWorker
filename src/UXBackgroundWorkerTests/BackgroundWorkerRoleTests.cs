using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
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

            testee.OnStart();
            Task.Factory.StartNew(testee.Run);
            Thread.Sleep(100);
            testee.OnStop();

            Assert.IsTrue(SimpleStarter.HasBeenCalled);
        }

        class TestableWorker : BackgroundWorkerRole
        {
            protected override bool OnRoleStarted()
            {
                return true;
            }

            protected override int TaskTimeout
            {
                get
                {
                    return 1;
                }
            }
        }

        public class SimpleStarter : IStartupTask
        {
            public static bool HasBeenCalled { get; private set; }

            public void Start()
            {
                HasBeenCalled = true;
            }
        }

        public class SimpleWorker : BaseWorker
        {
            protected override void Process()
            {
            }
        }

    }
}
