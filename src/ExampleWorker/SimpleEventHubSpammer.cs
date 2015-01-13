using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleEventHubSpammer : BaseWorker
    {
        private readonly EventHubClient _eventHubClient;

        private readonly string[] _pks =
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        };

        private readonly Random _rnd = new Random();

        public SimpleEventHubSpammer()
        {
            var connectionString = CloudConfigurationManager.GetSetting("ServiceBusConnectionString");

            _eventHubClient = EventHubClient.CreateFromConnectionString(connectionString, "Simpletest");
        }

        protected override async Task StartAsync()
        {
            var count = _rnd.Next(0, 4);
            var logEntry = new LogEntry {Message = _pks[count]};
            var byteArray = LogEntry.AsByteArray(logEntry);
            var eventData = new EventData(byteArray) {PartitionKey = logEntry.Message};
            await _eventHubClient.SendAsync(eventData);
        }
    }

    [Serializable]
    public class LogEntry
    {
        public Guid Id { get; set; }

        public DateTime Date { get; set; }

        public string Message { get; set; }

        public static byte[] AsByteArray(LogEntry entry)
        {
            var memoryStream = new MemoryStream();
            var binaryFormatter = new BinaryFormatter();

            binaryFormatter.Serialize(memoryStream, entry);
            return memoryStream.ToArray();
        }

        public static LogEntry FromByteArray(byte[] entry)
        {
            var memoryStream = new MemoryStream(entry);
            var binaryFormatter = new BinaryFormatter();

            return binaryFormatter.Deserialize(memoryStream) as LogEntry;
        }


        public static LogEntry FromStream(Stream stream)
        {
            var binaryFormatter = new BinaryFormatter();

            return binaryFormatter.Deserialize(stream) as LogEntry;
        }
    }
}