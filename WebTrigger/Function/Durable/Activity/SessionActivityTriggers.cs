using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.WebJobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebTrigger.Model;
using WebTrigger.Service;

namespace WebTrigger.Function.Durable.Activity
{
    public static class SessionActivityTriggers
    {
        public static readonly string DBConnectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString")!;
        public static readonly string ConnectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        public static readonly string SessionContainer = "session";
        public static readonly SessionService sessionService;
        public static readonly NotificationService notificationService;
        public static readonly UserService userService;

        // Other configurations
        public static readonly string DatabaseId = Environment.GetEnvironmentVariable("DatabaseId")!;
        public static readonly string ContainerName = Environment.GetEnvironmentVariable("TaskContainer")!;
        public static readonly string SendGridApiKey = Environment.GetEnvironmentVariable("SENDGRID_APIKEY")!;
        public static readonly string TwilioAccountSid = Environment.GetEnvironmentVariable("ACCOUNT_SID")!;
        public static readonly string TwilioAuthToken = Environment.GetEnvironmentVariable("AUTH_TOKEN")!;

        static SessionActivityTriggers()
        {
            sessionService =new(new CosmosDbService<Session>(DBConnectionString,DatabaseId,SessionContainer));
            notificationService =new(SendGridApiKey,TwilioAccountSid,TwilioAuthToken);
            userService =new(ConnectionString,"User");
        }
        [FunctionName("ValidateSession")]
        public static async Task<bool> ValidateSession([ActivityTrigger] string sessionId)
        {
            // Replace with actual session validation logic
            Console.WriteLine($"Validating session: {sessionId}");
            return await sessionService.ValidateSessionAsync(sessionId);
        }

    }
}
