using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebTrigger.Model
{
    public class HttpandTableResponse<T>
    {
        public HttpResponseData? data { get; set; }
        public IActionResult? actionResult { get; set; }
        [TableOutput("User", Connection = "AzureWebJobsStorage")]
        public T? TableOutput { get; set; }
        [QueueOutput("url", Connection = "AzureWebJobsStorage")]
        public Queue? QueueOutput { get; set; }
        [QueueOutput("notificationqueue", Connection = "AzureWebJobsStorage")]
        public Email? NotificationQueueOutput { get; set; }

    }

}
