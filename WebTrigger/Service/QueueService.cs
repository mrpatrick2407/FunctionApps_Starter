using Azure.Storage.Queues;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebTrigger.Service.IService;

namespace WebTrigger.Service
{
    public class QueueService:IUrlQueueService,INotificationQueueService,IDeviceQueueService
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
        public async Task SendBulkMessagesAsync(List<string> messages)
        {
            await Parallel.ForEachAsync(messages, async (message, _) =>
            {
                await _queueClient.SendMessageAsync(Convert.ToBase64String(Encoding.UTF8.GetBytes(message)));
            });
        }
    }
}
