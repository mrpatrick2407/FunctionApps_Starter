using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebTrigger.Model;
using WebTrigger.Service;
using WebTrigger.Service.IService;
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.Configure<KestrelServerOptions>(options => options.AllowSynchronousIO = true);
        services.AddHttpClient();
        services.AddMvc();
        string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage")!;
        services.AddSingleton(new UserService(connectionString, "User"));
        services.AddSingleton<IUrlQueueService>(new QueueService(connectionString, "url"));
        services.AddSingleton<INotificationQueueService>(new QueueService(connectionString, "notificationqueue"));
        services.AddSingleton<IImageBlobService>(new BlobService(connectionString, "ppimage"));
        services.AddSingleton<IImageSmallBlobService>(new BlobService(connectionString, "ppsmall"));
        services.AddSingleton<IImageMediumBlobService>(new BlobService(connectionString, "ppmedium"));
        services.AddSingleton<INotificationBlobService>(new BlobService(connectionString, "userregistrationemaillogs"));
        string DBconnectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString")!;
        string databaseid = Environment.GetEnvironmentVariable("DatabaseId")!;
        string containerName = Environment.GetEnvironmentVariable("CosmosContainerName")!;
        services.AddSingleton(new SessionService(new(DBconnectionString, databaseid, containerName)));
        services.AddSingleton(new NotificationService(Environment.GetEnvironmentVariable("SENDGRID_APIKEY")!, Environment.GetEnvironmentVariable("ACCOUNT_SID")!,
            Environment.GetEnvironmentVariable("AUTH_TOKEN")!));
    })
    .Build();

host.Run();
