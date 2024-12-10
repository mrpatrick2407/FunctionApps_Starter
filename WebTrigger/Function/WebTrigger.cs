using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Net.Http.Json;
using System.Text;
using WebTrigger.Model;
using WebTrigger.Service;
using WebTrigger.Service.IService;
namespace WebTrigger.Function
{
    public class WebTrigger
    {
        private readonly ILogger<WebTrigger> _logger;
        private readonly HttpClient _client;
        private UserService _userService;
        private IUrlQueueService _urlQueueService;
        private INotificationQueueService _notificationQueueService;
        private IImageBlobService _imageBlobService;
        private IImageSmallBlobService _imageSmallBlobService;
        private IImageMediumBlobService _imageMediumBlobService;
        private SessionService _sessionService;
        public WebTrigger(HttpClient client,UserService userService,SessionService sessionService,IUrlQueueService urlQueueService,INotificationQueueService notificationQueueService,IImageBlobService imageBlobService,IImageMediumBlobService imageMediumBlobService,IImageSmallBlobService imageSmallBlobService, ILogger<WebTrigger> logger)
        {
            _client = client;
            _logger = logger;
            _urlQueueService = urlQueueService;
            _notificationQueueService = notificationQueueService;
            _userService = userService;
            _imageBlobService = imageBlobService;
            _imageMediumBlobService = imageMediumBlobService;
            _imageSmallBlobService = imageSmallBlobService;
            _sessionService = sessionService;
        }

        [Function("RegisterUser")]
        public async Task<IActionResult> RegisterUser(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, [Microsoft.Azure.Functions.Worker.Http.FromBody]User user, FunctionContext context)
        {
            var userExists = await _userService.UserExists(user.email!);
            if (userExists)
                return new BadRequestObjectResult(new { message= "User already registered" });
            else
            {
                await _userService.RegisterUser(user);
                await _urlQueueService.SendMessageAsync(JsonConvert.SerializeObject(new Queue()
                {
                    ProfilePicName = user.firstName,
                    ProfilePicUrl = user.profilePicUrl
                }));
                await _notificationQueueService.SendMessageAsync(JsonConvert.SerializeObject(new Email()
                {
                    FirstName = user.firstName,
                    LastName = user.lastName,
                    EmailRecipient = user.email,
                    RowKey = user.RowKey,
                    Phone=user.phone
                }));
                await _sessionService.CreateSessionAsync(user.RowKey!);
                return new JsonResult(new { message = "Registered Successfully", data = user }) { StatusCode = 200 };
            }
        }


        [Function("ProfilePictrigger")]

        public async Task ProfilePictrigger([QueueTrigger("url", Connection = "AzureWebJobsStorage")] Queue queue, 
    FunctionContext context)
        {

            string imageUrl = queue.ProfilePicUrl!;
            string blobName = $"{queue.ProfilePicName}/{queue.ProfilePicName}-{Guid.NewGuid()}.jpeg";
            try
            {
                var imageBytes = await _client.GetByteArrayAsync(imageUrl);
                await _imageBlobService.UploadAsync(imageBytes, _imageBlobService.GetBlobClient(blobName));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
                throw;
            }
        }

        [Function("BlobTrigger")]
        public async Task BlobTrigger([BlobTrigger("ppimage/{blobPath}")] Stream blob, string blobPath, FunctionContext context)
        {
            string[] path = blobPath.Split('/');
            var SmallBlobName = $"{path[0]}/{path[0]}-{Guid.NewGuid()}.jpeg";
            var MediumBlobName = $"{path[0]}/{path[0]}-{Guid.NewGuid()}.jpeg";
            IImageFormat imageFormat = Image.DetectFormat(blob);

            await _imageSmallBlobService.ResizeImageAndSaveAsync(blob, ImageModel.ImageSize.Small, imageFormat,SmallBlobName);
            await _imageMediumBlobService.ResizeImageAndSaveAsync(blob, ImageModel.ImageSize.Medium, imageFormat,SmallBlobName);
        }

    }
}
