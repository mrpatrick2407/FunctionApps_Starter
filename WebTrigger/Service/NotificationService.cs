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


    }
}
