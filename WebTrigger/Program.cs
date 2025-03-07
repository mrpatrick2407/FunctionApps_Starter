using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.Identity.Web;
using Newtonsoft.Json;
using StaticHelper;
using WebTrigger.Model;
using WebTrigger.Service;
using WebTrigger.Service.IService;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration(builder =>
    {
        string cs = Environment.GetEnvironmentVariable("AzureAppConfigurationConnectionString")!;
        builder.AddAzureAppConfiguration(options =>
        {
            options.Connect(cs)
                   .UseFeatureFlags(); // Enable feature flags support
        });
    })
    .ConfigureServices(services =>
    {
        var authority = $"{Environment.GetEnvironmentVariable("AzureAdB2C__Instance")}/{Environment.GetEnvironmentVariable("AzureAdB2C__SignUpSignInPolicyId")}";
        var clientId = Environment.GetEnvironmentVariable("AzureAdB2C__ClientId");
        var audience = Environment.GetEnvironmentVariable("AzureAdB2C__Audience");
        services.AddApplicationInsightsTelemetryWorkerService(option=>option.ConnectionString= Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"));
        services.ConfigureFunctionsApplicationInsights();
        services.Configure<KestrelServerOptions>(options => options.AllowSynchronousIO = true);
        services.AddHttpClient();
        services.AddMvc();
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
 
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "AzureWebJobsStorage is missing.");
        }
        services.AddSingleton<IUserService>(new UserService(connectionString, "User"));
        services.AddSingleton<IUrlQueueService>(new QueueService(connectionString, "url"));
        services.AddSingleton<INotificationQueueService>(new QueueService(connectionString, "notificationqueue"));
        services.AddSingleton<IDeviceQueueService>(new QueueService(connectionString, "devicequeue"));
        services.AddSingleton<IImageBlobService>(new BlobService(connectionString, "ppimage"));
        services.AddSingleton<IImageSmallBlobService>(new BlobService(connectionString, "ppsmall"));
        services.AddSingleton<IImageMediumBlobService>(new BlobService(connectionString, "ppmedium"));
        services.AddSingleton<INotificationBlobService>(new BlobService(connectionString, "userregistrationemaillogs"));
        string DBconnectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString")!;
        string databaseid = Environment.GetEnvironmentVariable("DatabaseId")!;
        string containerName = Environment.GetEnvironmentVariable("SessionContainerName")!;
        services.AddSingleton<ISessionService>(new SessionService(new(DBconnectionString, databaseid, containerName)));
        services.AddSingleton(new NotificationService(Environment.GetEnvironmentVariable("SENDGRID_APIKEY")!, Environment.GetEnvironmentVariable("ACCOUNT_SID")!,
            Environment.GetEnvironmentVariable("AUTH_TOKEN")!));
        services.AddSingleton(new TaskService(new CosmosDbService<TaskModel>(DBconnectionString,databaseid,"task"), new CosmosDbService<EscalateTask>(DBconnectionString, databaseid, "escalatetask")));
        services.AddSingleton<AIService>(sp =>
        {
            var telemetryClient = sp.GetRequiredService<TelemetryClient>();
            var logger = sp.GetRequiredService<ILogger<AIService>>();
            var aiAppKey = Environment.GetEnvironmentVariable("AI_APP_KEY");
            var aiAppId = Environment.GetEnvironmentVariable("AI_APP_ID");
            var appInsightsApi = Environment.GetEnvironmentVariable("AI_API");
            return new AIService(telemetryClient, logger, aiAppKey!, aiAppId!, appInsightsApi!);
        });
        //services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApi(options =>
        //{
        //    options.Authority = authority;
        //    options.Audience = audience;
        //}, options => { });
        //services.AddAuthorization(); 
        services.AddAzureAppConfiguration();
        services.AddFeatureManagement();
    })
    .Build();

host.Run();
