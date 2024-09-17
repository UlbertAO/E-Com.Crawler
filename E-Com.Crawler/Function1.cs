using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace E_Com.Crawler
{
    public class Function1
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly StorageManager _storageManager;

        public Function1(IConfiguration configuration, ILoggerFactory loggerFactory, StorageManager storageManager)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<Function1>();
            _storageManager = storageManager;
        }

        [Function("Function1")]
        public async Task Run([TimerTrigger("0 0 6 * * *", RunOnStartup = true)] TimerInfo timer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            try
            {
                await _storageManager.CreateContainer();
                if (_storageManager._containerClient == null)
                {
                    throw new Exception("Container not created");
                }

                var urls = Environment.GetEnvironmentVariable("Ecom_Urls")?.Split(';');
                if (urls == null)
                {
                    _logger.LogError("No eCommerce URLs found in environment variables.");
                    return;
                }
                var productHtmls = new List<string>();
                foreach (var url in urls)
                {
                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            var response = await httpClient.GetAsync(url);
                            response.EnsureSuccessStatusCode();

                            var html = await response.Content.ReadAsStringAsync();

                            productHtmls.Add(html);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error scraping URL {url}: {ex.Message}");
                    }
                }

                var count = 0;
                foreach (var html in productHtmls) { 
                    await _storageManager.UploadBlob($"html_{count}", html);
                    count++;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);

            }
            finally
            {
                if (timer.ScheduleStatus is not null)
                {
                    _logger.LogInformation($"Next timer schedule at: {timer.ScheduleStatus.Next}");
                }
            }

        }
    }
}
