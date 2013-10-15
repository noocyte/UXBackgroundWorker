using Microsoft.WindowsAzure;
using System.Net.Http;
using UXBackgroundWorker;

namespace ExampleWorker
{
    public class SimpleServicebusWorker : BaseServiceBusWorker
    {
        protected override string ConnectionString
        {
            get { return CloudConfigurationManager.GetSetting("TopicConnectionString"); }
        }

        protected override void Do(string message)
        {
            var client = new HttpClient();
            client.GetAsync("http://blog.noocyte.net");
        }

        protected override string TopicName
        {
            get { return "SimpleTopic"; }
        }
    }
}
