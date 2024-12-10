using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Reactive;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using WebTrigger.Model;
using WebTrigger.Service;
using WebTrigger.Service.IService;

namespace WebTrigger.Function
{
    public class SendEmail
    {
        private readonly ILogger<SendEmail> _logger;
        private readonly INotificationBlobService _notificationBlobService;
        private readonly NotificationService _notificationService;
        public SendEmail(ILogger<SendEmail> logger,INotificationBlobService notificationBlobService,NotificationService notificationService)
        {
            _notificationBlobService = notificationBlobService;
            _notificationService = notificationService;
            _logger = logger;
        }

        [Function("SendEmailService")]
        public async Task Run([QueueTrigger("notificationqueue", Connection = "AzureWebJobsStorage")] Email queueItem)
        {
            var blobName = $"{queueItem.RowKey}.log";
            await _notificationService.SendMail(queueItem);
            await _notificationService.SendMessage(new() { PhoneMessage = await _notificationService.GetTextTemplateContent("SMS", "UserRegistration", queueItem), PhoneNumber=queueItem.Phone});
            var EmailContent = await _notificationService.GetTemplateContent("Email", "UserRegistration", queueItem);
            await _notificationBlobService.UploadAsync(Encoding.UTF8.GetBytes(EmailContent), blobName);
        }

    }
}
