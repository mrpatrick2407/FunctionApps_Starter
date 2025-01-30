using Azure.Data.Tables;
using Azure.Storage.Blobs;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Text;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using WebTrigger.Model;
using WebTrigger.Service.IService;

namespace WebTrigger.Service
{
    public class NotificationService:IMessageService,IMailService
    {
        private readonly SendGridClient? _sendGridClient;
        public NotificationService(string apiKey , string AccountSID, string auth_token) { 
        _sendGridClient = new SendGridClient(apiKey);
        TwilioClient.Init(AccountSID, auth_token);
        }
        public async Task SendMail(Email notification)
        {

            EmailAddress from=new EmailAddress(Environment.GetEnvironmentVariable("Sender_Email"));
            EmailAddress to=new EmailAddress(notification.EmailRecipient);
            var subject = "Registration Successful";
            var body =await GetTextTemplateContent("SMS", "UserRegistration",notification);
            var emailContent =await GetTemplateContent("Email", "UserRegistration", notification);
            Attachment attachment = new Attachment();
            attachment.Content = Convert.ToBase64String(Encoding.ASCII.GetBytes(emailContent));
            attachment.Disposition = "attachment";
            attachment.Filename = "EmailLog.html";
            attachment.Type = "text/plain";
            var mail = MailHelper.CreateSingleEmail(from, to, subject, body, emailContent);
            mail.AddAttachment(attachment);
            await _sendGridClient!.SendEmailAsync(mail);
        }
        public async Task SendMessage(Phone phone)
        {
            var message = await MessageResource.CreateAsync(
                body: phone.PhoneMessage,
                from: new Twilio.Types.PhoneNumber("+16814335239"),
                to: new Twilio.Types.PhoneNumber($"+91{phone.PhoneNumber}")
                );
            Console.Write($"Message to 916382723256 has been {message.Status}.");
        }
        public async Task UploadAsync(byte[] imagebytes, BlobClient client)
        {
            using var memstream = new MemoryStream(imagebytes);
            await client.UploadAsync(memstream);
        }


        public async Task<string> GetTemplateContent(string templateType, string templateName,Email email)
            {
                // Build the template file path
                var templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates", templateType, $"{templateName}.html"); // Or .txt for SMS

                if (!File.Exists(templatePath))
                    throw new FileNotFoundException($"Template not found: {templatePath}");

                var templateContent = await File.ReadAllTextAsync(templatePath);
            var stringFormat = templateContent.Replace("{{FirstName}}", email.FirstName).Replace("{{LastName}}", email.LastName).Replace("{{Email}}",email.EmailRecipient);

            return stringFormat;
            }
        public async Task<string> GetTextTemplateContent(string templateType, string templateName,Email email)
        {
            // Build the template file path
            var current = Directory.GetCurrentDirectory();
            var templatePath = Path.Combine(AppContext.BaseDirectory,"..","..","..", "Templates", templateType, $"{templateName}.txt"); // Or .txt for SMS
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template not found: {templatePath}");

            var templateContent = await File.ReadAllTextAsync(templatePath);

            return string.Format(templateContent, email.FirstName, email.EmailRecipient);
        }
        public async Task SendEscalationMail(TaskModel task, Email notification)
        {
            EmailAddress from = new EmailAddress(Environment.GetEnvironmentVariable("Sender_Email"));
            EmailAddress to = new EmailAddress(notification.EmailRecipient);
            var subject = "Task Escalation Alert";
            var body = await GetTextTemplateContent("SMS", "EscalationTask", notification, task);
            var emailContent = await GetTemplateContent("Email", "EscalationTask", notification, task);

            var mail = MailHelper.CreateSingleEmail(from, to, subject, body, emailContent);
            await _sendGridClient!.SendEmailAsync(mail);
        }

        public async Task SendPriorityTaskMail(TaskModel task, Email notification)
        {
            EmailAddress from = new EmailAddress(Environment.GetEnvironmentVariable("Sender_Email"));
            EmailAddress to = new EmailAddress(notification.EmailRecipient);
            var subject = "High Priority Task Alert";
            var body = await GetTextTemplateContent("SMS", "PriorityTask", notification, task);
            var emailContent = await GetTemplateContent("Email", "PriorityTask", notification, task);

            var mail = MailHelper.CreateSingleEmail(from, to, subject, body, emailContent);
            await _sendGridClient!.SendEmailAsync(mail);
        }

        public async Task<string> GetTemplateContent(string templateType, string templateName, Email email, TaskModel task)
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates", templateType, $"{templateName}.html");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template not found: {templatePath}");

            var templateContent = await File.ReadAllTextAsync(templatePath);
            return templateContent
                .Replace("{{FirstName}}", email.FirstName)
                .Replace("{{LastName}}", email.LastName)
                .Replace("{{Email}}", email.EmailRecipient)
                .Replace("{{Title}}", task.Title)
                .Replace("{{Description}}", task.Description)
                .Replace("{{Deadline}}", task.Deadline?.ToString("yyyy-MM-dd HH:mm") ?? "N/A")
                .Replace("{{Priority}}", task.Priority);
        }

        public async Task<string> GetTextTemplateContent(string templateType, string templateName, Email email, TaskModel task)
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates", templateType, $"{templateName}.txt");
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template not found: {templatePath}");

            var templateContent = await File.ReadAllTextAsync(templatePath);
            return templateContent
                .Replace("{{FirstName}}", email.FirstName)
                .Replace("{{Title}}", task.Title)
                .Replace("{{Deadline}}", task.Deadline?.ToString("yyyy-MM-dd HH:mm") ?? "N/A")
                .Replace("{{Priority}}", task.Priority);
        }
        public async Task SendPriorityTaskEmail(TaskModel task)
        {
            var from = new EmailAddress(Environment.GetEnvironmentVariable("Sender_Email"));
            var to = new EmailAddress("recipient@example.com"); // You can replace this with the task assignee's email or dynamic assignment.
            var subject = $"Priority Task: {task.Title} - Action Needed";

            var body = "";
            var emailContent = await GetTemplateContentForPriorityTask("Email", "PriorityTask", task);

            var attachment = new Attachment
            {
                Content = Convert.ToBase64String(Encoding.ASCII.GetBytes(emailContent)),
                Disposition = "attachment",
                Filename = "PriorityTaskLog.html",
                Type = "text/html"
            };

            var mail = MailHelper.CreateSingleEmail(from, to, subject, body, emailContent);
            mail.AddAttachment(attachment);

            await _sendGridClient!.SendEmailAsync(mail);
        }
        public async Task SendTaskSummaryEmail(int pendingTasks, int updatedTasks, int priorityTasks,string Recipient,string name)
        {
            var from = new EmailAddress(Environment.GetEnvironmentVariable("Sender_Email"));
            var to = new EmailAddress(Recipient); // Update this dynamically

            var subject = "Your Task Summary";

            var emailContent = await GetTemplateContentForTaskSummary("Email", "TaskSummary",name, Recipient, pendingTasks, updatedTasks, priorityTasks);

            var mail = MailHelper.CreateSingleEmail(from, to, subject, "", emailContent);

            await _sendGridClient!.SendEmailAsync(mail);
        }
        public async Task SendEscalationTaskEmail(TaskModel task)
        {
            var from = new EmailAddress(Environment.GetEnvironmentVariable("Sender_Email"));
            var to = new EmailAddress("recipient@example.com"); // You can replace this with the task assignee's email or dynamic assignment.
            var subject = $"Escalation: {task.Title} - Immediate Action Required";

            var body = "";
            var emailContent = await GetTemplateContentForEscalationTask("Email", "EscalationTask", task);

            var attachment = new Attachment
            {
                Content = Convert.ToBase64String(Encoding.ASCII.GetBytes(emailContent)),
                Disposition = "attachment",
                Filename = "EscalationTaskLog.html",
                Type = "text/html"
            };

            var mail = MailHelper.CreateSingleEmail(from, to, subject, body, emailContent);
            mail.AddAttachment(attachment);

            await _sendGridClient!.SendEmailAsync(mail);
        }

        public async Task<string> GetTemplateContentForPriorityTask(string templateType, string templateName, TaskModel task)
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates", templateType, $"{templateName}.html");

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template not found: {templatePath}");

            var templateContent = await File.ReadAllTextAsync(templatePath);
            var stringFormat = templateContent.Replace("{{TaskTitle}}", task.Title)
                                              .Replace("{{TaskDescription}}", task.Description)
                                              .Replace("{{TaskDeadline}}", task.Deadline?.ToString("MM/dd/yyyy HH:mm"))
                                              .Replace("{{Priority}}", task.Priority ?? "Normal");

            return stringFormat;
        }

        public async Task<string> GetTemplateContentForEscalationTask(string templateType, string templateName, TaskModel task)
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates", templateType, $"{templateName}.html");

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template not found: {templatePath}");

            var templateContent = await File.ReadAllTextAsync(templatePath);
            var stringFormat = templateContent.Replace("{{TaskTitle}}", task.Title)
                                              .Replace("{{TaskDescription}}", task.Description)
                                              .Replace("{{TaskDeadline}}", task.Deadline?.ToString("MM/dd/yyyy HH:mm"))
                                              .Replace("{{EscalationReason}}", "Task has not been completed and is being escalated due to approaching deadline.");

            return stringFormat;
        }
       

        private async Task<string> GetTemplateContentForTaskSummary(string templateType, string templateName,string name, string Recipient, int pendingTasks, int updatedTasks, int priorityTasks)
        {
            string templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates", templateType, $"{templateName}.html");

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template not found: {templatePath}");

            string templateContent = await File.ReadAllTextAsync(templatePath);

            return templateContent
                .Replace("{{UserName}}", name ?? "User")
                .Replace("{{PendingTasks}}", pendingTasks.ToString())
                .Replace("{{UpdatedTasks}}", updatedTasks.ToString())
                .Replace("{{PriorityTasks}}", priorityTasks.ToString());
        }
        public async Task SendApplicationInsightSummaryEmail(ApplicationInsightResult result, string appName, string emailRecipient)
        {
            var from = new EmailAddress(Environment.GetEnvironmentVariable("Sender_Email"));
            var to = new EmailAddress(emailRecipient);
            var subject = $"{appName} - Daily Telemetry Report";
            var body = "";
            var emailContent = await GetTemplateContentForApplicationInsightSummary("Email", "ApplicationInsightSummary", result, appName);
            var attachment = new Attachment
            {
                Content = Convert.ToBase64String(Encoding.ASCII.GetBytes(emailContent)),
                Disposition = "attachment",
                Filename = "ApplicationInsightSummary.html",
                Type = "text/plain"
            };
            var mail = MailHelper.CreateSingleEmail(from, to, subject, body, emailContent);
            mail.AddAttachment(attachment);
            await _sendGridClient!.SendEmailAsync(mail);
        }

        public async Task<string> GetTemplateContentForApplicationInsightSummary(string templateType, string templateName, ApplicationInsightResult result, string appName)
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Templates", templateType, $"{templateName}.html");

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Template not found: {templatePath}");

            var templateContent = await File.ReadAllTextAsync(templatePath);
            var stringFormat = templateContent.Replace("{appName}", appName)
                                              .Replace("{today}", DateTime.UtcNow.ToString("yyyy-MM-dd"))
                                              .Replace("{result.TotalRequests}", result.TotalRequests.ToString())
                                              .Replace("{result.FailedRequests}", result.FailedRequests.ToString())
                                              .Replace("{result.TotalExceptions}", result.TotalExceptions.ToString());

            return stringFormat;
        }
    }
}
