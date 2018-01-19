﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Ninject.Extensions.Azure.Clients;
using Proactima.AzureWorkers;

namespace ExampleWorker
{
    public class SimpleServiceBusQueueWorker : BaseServiceBusQueueWorker
    {
        public SimpleServiceBusQueueWorker(ICreateClientsAsync clientFactory)
            : base(clientFactory)
        {
        }

        protected override string QueueName
        {
            get { return "RandomQueue"; }
        }
        public override bool Enabled
        {
            get { return false; }
        }
        protected override async Task Do(IEnumerable<BrokeredMessage> messages)
        {
            await DeleteMessages(messages).ConfigureAwait(false);
        }
    }
}