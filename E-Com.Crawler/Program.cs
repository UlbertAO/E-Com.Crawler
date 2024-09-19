using E_Com.Crawler;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

try
{
    var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddScoped<StorageManager>();
        services.AddScoped<CrawlerManager>();
        services.AddScoped<ProductLinkManager>();
        services.AddScoped<Utilities>();


    })
    .Build();

    host.Run();

}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}
