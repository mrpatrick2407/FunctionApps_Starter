using Azure.Storage.Queues;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTrigger.Service
{
    public class QueueService:IUrlQueueService,INotificationQueueService
    {
        public readonly QueueClient _queueClient;
        public QueueService(string connectionString,string queueName) {
            _queueClient = new QueueClient(connectionString, queueName);
        }
        public async Task SendMessageAsync(string message)
        {
            string serializedMessage = Convert.ToBase64String(Encoding.UTF8.GetBytes(message));
            await _queueClient.SendMessageAsync(serializedMessage);
        }
    }
}
