using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebTrigger.Service;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        var authority = $"{Environment.GetEnvironmentVariable("AzureAdB2C__Instance")}/{Environment.GetEnvironmentVariable("AzureAdB2C__SignUpSignInPolicyId")}";
        var clientId = Environment.GetEnvironmentVariable("AzureAdB2C__ClientId");
        var audience = Environment.GetEnvironmentVariable("AzureAdB2C__Audience");
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString), "AzureWebJobsStorage is missing.");
        }
        services.Configure<KestrelServerOptions>(options => options.AllowSynchronousIO = true);
        services.AddHttpClient();
        services.AddMvc();
        services.AddSingleton(new UserService(connectionString!, "User"));
        string DBconnectionString = Environment.GetEnvironmentVariable("CosmosDBConnectionString")!;
        string databaseid = Environment.GetEnvironmentVariable("DatabaseId")!;
        string containerName = Environment.GetEnvironmentVariable("SessionContainerName")!;
        services.AddSingleton(new SessionService(new(DBconnectionString, databaseid, containerName)));
    })
    .Build();

host.Run();
