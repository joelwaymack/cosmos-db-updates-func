using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        var cosmosClient = new CosmosClient(Environment.GetEnvironmentVariable("CosmosConnectionString"),
            new CosmosClientOptions { SerializerOptions = new CosmosSerializationOptions {  PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase } });
        var cosmosDatabase = cosmosClient.GetDatabase(Environment.GetEnvironmentVariable("DatabaseName"));
        var orderContainer = cosmosDatabase.GetContainer(Environment.GetEnvironmentVariable("OrderContainerName"));
        services.AddKeyedSingleton("OrderContainer", orderContainer);
    })
    .Build();

host.Run();
