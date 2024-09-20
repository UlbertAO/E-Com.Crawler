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
        private readonly CrawlerManager _crawlerManager;
        private readonly Utilities _utilities;

        public Function1(IConfiguration configuration, ILoggerFactory loggerFactory, StorageManager storageManager, CrawlerManager crawlerManager, Utilities utilities)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<Function1>();
            _storageManager = storageManager;
            _crawlerManager = crawlerManager;
            _utilities = utilities;

        }

        [Function("Function1")]
        public async Task Run([TimerTrigger("0 0 6 * * *", RunOnStartup = true)] TimerInfo timer)
        {
            _logger.LogInformation($"Crawler Timer trigger function execution started at: {DateTime.Now}");
            try
            {
                await _storageManager.CreateContainer();

                var urls = Environment.GetEnvironmentVariable("EcomUrls")?.Split(';');
                if (urls == null)
                {
                    var msg = "No URL found in environment variables.";
                    _logger.LogError(msg);
                    throw new Exception(msg);
                }
                foreach (var url in urls)
                {
                    try
                    {
                        if (_utilities.isValidUrl(url))
                        {
                            var productLinkTitleDict = await _crawlerManager.crawler(url);

                            var count = 1;
                            var totalCount = productLinkTitleDict.Count;

                            _logger.LogInformation($"Upload product HTML content started for {url}");

                            foreach (var keyValuePair in productLinkTitleDict)
                            {
                                await _storageManager.UploadBlob(new Uri(url).Host, keyValuePair.Value, await _crawlerManager.crawlerForContent(keyValuePair.Key));
                                _logger.LogInformation($"{count}/{totalCount}");
                                count++;
                            }
                            _logger.LogInformation($"Upload Ended");
                        }
                        else
                        {
                            throw new Exception($"{url} is not a valid URL");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error scraping URL {url}: {ex.Message}");
                    }
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
                    _logger.LogInformation($"Crawler Timer trigger function execution ended at: {DateTime.Now}");

                    _logger.LogInformation($"Next timer schedule at: {timer.ScheduleStatus.Next}");
                }
            }

        }
    }
}
