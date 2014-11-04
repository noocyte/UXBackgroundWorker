using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Proactima.AzureWorkers
{
    public interface ICreateClients
    {
        Task<CloudQueue> CreateStorageQueueClientAsync(string queueName);
        Task<QueueClient> CreateServicebusQueueClientAsync(string queueName);
        Task<TopicClient> CreateTopicClientAsync(string topicName);
        Task<SubscriptionClient> CreateSubscriptionClientAsync(string topicName, string subscriptionName);
    }
}