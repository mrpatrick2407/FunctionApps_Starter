using Azure.Core.Serialization;
using Grpc.Net.Client.Balancer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using UnitTests;
using WebTrigger.Function;
using WebTrigger.Model;
using WebTrigger.Service;
using WebTrigger.Service.IService;

[TestClass]
public class LoginUserTests
{

    
    private Mock<IUserService> _mockUserService;
    private Mock<ISessionService> _mockSessionService;
    private Mock<IConfiguration> _mockConfiguration;
    private Mock<HttpClient> _mockHttpClient;
    private Mock<ILogger<WebTriggerClass>> _mockLogger;
    private Mock<IFeatureManager> _mockFeatureManager;
    private WebTriggerClass _webTrigger;

    [TestInitialize]
    public void Setup()
    {
        _mockUserService = new Mock<IUserService>();
        _mockSessionService = new Mock<ISessionService>();
        _mockFeatureManager = new Mock<IFeatureManager>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockHttpClient = new Mock<HttpClient>();
        _mockLogger = new Mock<ILogger<WebTriggerClass>>();

        _webTrigger = new WebTriggerClass(
            _mockFeatureManager.Object,
            _mockConfiguration.Object,
            _mockHttpClient.Object,
            _mockUserService.Object,
            Mock.Of<IDeviceQueueService>(),
            _mockSessionService.Object ,
            Mock.Of<IUrlQueueService>(),
            Mock.Of<INotificationQueueService>(),
            Mock.Of<IImageBlobService>(),
            Mock.Of<IImageMediumBlobService>(),
            Mock.Of<IImageSmallBlobService>(),
            _mockLogger.Object);
    }


    private async Task<FakeHttpRequestData> CreateHttpRequest(string jsonContent)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton(Options.Create(new WorkerOptions { Serializer = new JsonObjectSerializer() }));
        serviceCollection.AddSingleton<ObjectSerializer,JsonObjectSerializer>();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var context = new Mock<FunctionContext>();
        context.Setup(c => c.InstanceServices).Returns(serviceProvider);
       

        //var req = new FakeHttpRequestData(context.Object, new Uri("http://localhost:7044/SubscribeFunc"), stream, jsonContent);
        //var response = req.CreateResponse();

        //// Example usage (your actual test logic here)
        //await response.WriteAsJsonAsync(new { message = "Active session found" });

        //string responseBody;
        //response.Body.Position = 0;
        //using (var reader = new StreamReader(response.Body, Encoding.UTF8))
        //{
        //    responseBody = await reader.ReadToEndAsync(); // Read stream to string
        //}

        //Console.WriteLine(responseBody); // Output the response body for verification

        //// Print the response body to the console or test output
        //Console.WriteLine(responseBody+"hj");
        ////var request = new Mock<HttpRequestData>(context.Object);
        
        ////request.Setup(r => r.Body).Returns(stream);
        ////request.Setup(r => r.ReadAsStringAsync(It.IsAny<Encoding?>()))
        ////  .ReturnsAsync(jsonContent);
        ////request.Setup(r => r.CreateResponse()).Returns(CreateMockResponse(context.Object));
        var requestData = new FakeHttpRequestData(
        context.Object,
        new Uri("http://localhost:7044/SubscribeFunc"),
        stream,
        jsonContent // Pass the JSON content for ReadAsStringAsync
    );

        return requestData;
    }

    [TestMethod]
    public async Task LoginUser_ShouldReturnBadRequest_WhenEmailIsEmpty()
    {
        var request = await CreateHttpRequest("{}");
        var response = await _webTrigger.LoginUser(request, Mock.Of<FunctionContext>());
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task LoginUser_ShouldReturnActiveSession_WhenSessionExists()
    {
        var loginRequest = new { Email = "test@example.com" };
        _mockUserService.Setup(s => s.GetUserByEmail("test@example.com"))
                        .ReturnsAsync(new User { RowKey = "user123" });
        _mockSessionService.Setup(s => s.GetActiveSessionAsync("user123"))
                           .ReturnsAsync(true);

        var request = await CreateHttpRequest(JsonConvert.SerializeObject(loginRequest));
        var response = await _webTrigger.LoginUser(request, Mock.Of<FunctionContext>());

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task LoginUser_ShouldCreateNewSession_WhenNoActiveSessionExists()
    {
        var loginRequest = new { Email = "test@example.com" };
        _mockUserService.Setup(s => s.GetUserByEmail("test@example.com"))
                        .ReturnsAsync(new User { RowKey = "user123" });
        _mockSessionService.Setup(s => s.GetActiveSessionAsync("user123"))
                           .ReturnsAsync(false);
        _mockSessionService.Setup(s => s.CreateSessionAsync("user123"))
                           .ReturnsAsync("session456");

        var request = await CreateHttpRequest(JsonConvert.SerializeObject(loginRequest));
        var response = await _webTrigger.LoginUser(request, Mock.Of<FunctionContext>());

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
