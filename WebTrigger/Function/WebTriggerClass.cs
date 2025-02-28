using Azure.Storage.Queues.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using StaticHelper;
using System.Diagnostics;
using System.Net;
using System.Text;
using WebTrigger.Model;
using WebTrigger.Service;
using WebTrigger.Service.IService;
namespace WebTrigger.Function
{
    public class WebTriggerClass
    {
        private readonly ILogger<WebTriggerClass> _logger;
        private readonly HttpClient _client;
        private IUserService _userService;
        private readonly IFeatureManager _featureManager;
        private IUrlQueueService _urlQueueService;
        private IDeviceQueueService _deviceQueueService;
        private INotificationQueueService _notificationQueueService;
        private IImageBlobService _imageBlobService;
        private IImageSmallBlobService _imageSmallBlobService;
        private IImageMediumBlobService _imageMediumBlobService;
        private ISessionService _sessionService;
        private readonly IConfiguration _configuration;
        public WebTriggerClass(IFeatureManager featureManager, IConfiguration configuration,HttpClient client,IUserService userService,IDeviceQueueService deviceQueueService,ISessionService sessionService,IUrlQueueService urlQueueService,INotificationQueueService notificationQueueService,IImageBlobService imageBlobService,IImageMediumBlobService imageMediumBlobService,IImageSmallBlobService imageSmallBlobService, ILogger<WebTriggerClass> logger)
        {
            _client = client;
            _logger = logger;
            _featureManager= featureManager;
            _urlQueueService = urlQueueService;
            _notificationQueueService = notificationQueueService;
            _userService = userService;
            _imageBlobService = imageBlobService;
            _imageMediumBlobService = imageMediumBlobService;
            _imageSmallBlobService = imageSmallBlobService;
            _sessionService = sessionService;
            _deviceQueueService = deviceQueueService;
            _configuration = configuration;
        }

        [Function("UserRegister")]
        public async Task<HttpResponseData> RegisterUser(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, [Microsoft.Azure.Functions.Worker.Http.FromBody]User user, FunctionContext context)
        {
            var userExists = await _userService.UserExists(user.email!);
            var response = req.CreateResponse();
            if (userExists)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteAsJsonAsync(new { message = "User already registered" });
                return response;
            }
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
                var sessionId=await _sessionService.CreateSessionAsync(user.RowKey!);
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new { message = "Registered Successfully", data = user });
                response.Headers.Add("sessionId",sessionId);
                return response;
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
        [Function("LoginUser")]
        public async Task<HttpResponseData> LoginUser(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
    FunctionContext context)
        {
            var requestBody = await req.ReadAsStringAsync();
            var loginRequest = JsonConvert.DeserializeObject<LoginRequest>(requestBody!);
            var response = req.CreateResponse();

            if (loginRequest == null || string.IsNullOrEmpty(loginRequest.Email))
            {
                response.StatusCode= HttpStatusCode.BadRequest;
                return response;
            }
            var getUserId =  (_userService.GetUserByEmail(loginRequest.Email)).Result!.RowKey;
            var existingSession = await _sessionService.GetActiveSessionAsync(getUserId!);

            if (existingSession)
            {
                response.StatusCode = HttpStatusCode.OK;
                await response.WriteAsJsonAsync(new { message = "Active session found" });
                return response;
            }
            else
            {
                var newSessionId = await _sessionService.CreateSessionAsync(getUserId!);
                response.StatusCode = HttpStatusCode.OK;
                response.Headers.Add("sessionId", newSessionId);
                await response.WriteAsJsonAsync(new { message = "New session created" });
                return response;
            }
        }
        [Function("DeviceQueueMessage")]
        public async Task<HttpResponseData> DeviceQueueMessage(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req, [Microsoft.Azure.Functions.Worker.Http.FromBody] IEnumerable<Device> user, FunctionContext context)
        {
            List<string> userStringList = user.Select(x =>JsonConvert.SerializeObject(x)).ToList();
            await _deviceQueueService.SendBulkMessagesAsync(userStringList);
            return req.CreateResponse(HttpStatusCode.OK);
        }
        [Function("HttpAlive")]
        public async Task<HttpResponseData> HttpAlive(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context)
        {
            var res= req.CreateResponse(HttpStatusCode.OK);
            await res.WriteAsJsonAsync(new { message="Avoding Cold start"});
            return res;
        }
        [Function("FeatureFlagTest")]
        public async Task<HttpResponseData> FeatureFlagTest(
            [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req, FunctionContext context)
        {
            string keyName = "CookbookApP:Settings:Greeting";
            string message = _configuration[keyName]!;
            var res = req.CreateResponse(HttpStatusCode.OK);
            if (await _featureManager.IsEnabledAsync("TurnOnGreeting"))
            {
                await res.WriteAsJsonAsync(new { message = message });
            }
            else
            {
                await res.WriteAsJsonAsync(new { message = "NoGreeting" });
            }
            return res;
        }
        [Function("HttpTriggerAvoidCS")]
        public async Task HttpTriggerAvoidCS([TimerTrigger("*/5 * * * *")] TimerInfo myTimer)
        {
           // await _client.GetAsync("https://azure-first.azurewebsites.net/api/HttpAlive"); 
            
        }
        [Function("DeviceQueueTrigger")]
        public void DeviceQueueTrigger(
        [QueueTrigger("devicequeue", Connection = "AzureWebJobsStorage")] QueueMessage message)
        {
            // Decode message body
            string messageBody = Encoding.UTF8.GetString(message.Body);

            // Log queue message metadata
            _logger.LogInformation($"Queue Payload: {messageBody}");
            _logger.LogInformation($"Message ID: {message.MessageId}");
            _logger.LogInformation($"Dequeue Count: {message.DequeueCount}");
            _logger.LogInformation($"Insertion Time: {message.InsertedOn}");
            _logger.LogInformation($"Expiration Time: {message.ExpiresOn}");
            _logger.LogInformation($"Next Visible Time: {message.NextVisibleOn}");
            _logger.LogInformation($"Pop Receipt: {message.PopReceipt}");
            throw new Exception("Checking Dequeue Count and then poisoned messages");
        }
        [Function("TimerFunction")]
        public async Task TimerFunction([TimerTrigger("0 */5 * * * *")] TimerInfo timer)
        {
            _logger.LogInformation($"Timer Trigger executed at: {DateTime.UtcNow}");

            //string exePath = Path.Combine(Directory.GetCurrentDirectory(), "win-x64", "TheoAPI.exe");

            //if (File.Exists(exePath))
            //{
            //    _logger.LogInformation($"Executing EXE: {exePath}");

            //    try
            //    {
            //        ProcessStartInfo psi = new ProcessStartInfo
            //        {
            //            FileName = exePath,
            //            RedirectStandardOutput = true,
            //            RedirectStandardError = true,
            //            UseShellExecute = false,
            //            CreateNoWindow = true
            //        };

            //        using (Process process = new Process { StartInfo = psi })
            //        {
            //            process.OutputDataReceived += (sender, args) => _logger.LogInformation(args.Data);
            //            process.ErrorDataReceived += (sender, args) => _logger.LogError(args.Data);

            //            process.Start();
            //            process.BeginOutputReadLine();
            //            process.BeginErrorReadLine();
            //            process.WaitForExit();
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        _logger.LogError($"Error running EXE: {ex.Message}");
            //    }
            //}
            //else
            //{
            //    _logger.LogError($"EXE file not found: {exePath}");
            //}
        }
    }
}
