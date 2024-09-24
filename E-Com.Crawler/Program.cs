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

        services.AddScoped<AppSetting>();

        services.AddScoped<StorageManager>();
        services.AddScoped<CrawlerManager>();
        services.AddScoped<ProductLinkManager>();
        services.AddScoped<Utilities>();

        Environment.SetEnvironmentVariable("PLAYWRIGHT_BROWSERS_PATH", Environment.GetEnvironmentVariable("HOME_EXPANDED"));
        Microsoft.Playwright.Program.Main(new string[] { "install", "chromium", "--with-deps", });

    })
    .Build();

    host.Run();

}
catch (Exception ex)
{
    Console.WriteLine(ex.ToString());
}
