using Microsoft.ServiceBus.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePoster
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "";
            string topicName = "serviceworker";
            var topicClient = TopicClient.CreateFromConnectionString(connectionString, topicName);
            topicClient.Send(new BrokeredMessage("test"));
        }
    }
}
