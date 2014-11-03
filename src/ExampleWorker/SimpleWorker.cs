//using System;
//using System.Diagnostics;
//using System.Net.Http;
//using System.Threading.Tasks;
//using Proactima.AzureWorkers;

//namespace ExampleWorker
//{
//    [Instance(20)]
//    public class SimpleWorker : BaseWorker
//    {
//        protected override async Task StartAsync()
//        {
//            var rnd = new Random();
//            await Task.Delay(rnd.Next(3000)).ConfigureAwait(false);
            
//            var r = rnd.Next(5);

//            if (r == 1)
//                throw new InvalidOperationException("Some random exception!");
//            InfoLogging("No exception!");
//            var client = new HttpClient();

//            if (!Token.IsCancellationRequested)
//                await client.GetAsync("http://blog.noocyte.net").ConfigureAwait(false);
//        }

//        protected override void InfoLogging(string message, string messageId = "")
//        {
//            Debug.WriteLine(message);
//        }

//        protected override void ErrorLogging(string message, Exception ex = null)
//        {
//            Debug.WriteLine(message);
//        }
//    }
//}