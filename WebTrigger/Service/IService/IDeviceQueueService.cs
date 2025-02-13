using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTrigger.Service.IService
{
    public interface IDeviceQueueService
    {
        /// <summary>
        /// Sends a message to the queue asynchronously.
        /// </summary>
        /// <param name="message">The message to be sent to the queue.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendMessageAsync(string message);
        Task SendBulkMessagesAsync(List<string> messages);
    }
}
