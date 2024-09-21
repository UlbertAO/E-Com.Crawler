using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace E_Com.Crawler
{
    public class CrawlerCron
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly StorageManager _storageManager;
        private readonly CrawlerManager _crawlerManager;
        private readonly Utilities _utilities;
        public CrawlerCron(IConfiguration configuration, ILoggerFactory loggerFactory, StorageManager storageManager, CrawlerManager crawlerManager, Utilities utilities)
        {
            _configuration = configuration;
            _logger = loggerFactory.CreateLogger<CrawlerCron>();
            _storageManager = storageManager;
            _crawlerManager = crawlerManager;
            _utilities = utilities;
        }

        [Function("CrawlerCron")]
        public async Task Run([TimerTrigger("0 0 6 * * *", RunOnStartup = true)] TimerInfo timer)
        {
            _logger.LogInformation($"Crawler Timer trigger function execution started at: {DateTime.Now}");
            try
            {
                await _storageManager.CreateContainer();

                var urls = Environment.GetEnvironmentVariable("EcomUrls")?.Split(';');
                if (urls == null)
                {
                    var msg = "URLs found in environment variables.";
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
                            _logger.LogInformation($"Found {productLinkTitleDict.Count} product URLs for {url}");

                            var count = 1;
                            var totalCount = productLinkTitleDict.Count;

                            _logger.LogInformation($"Uploading product HTML files to blob for URL: {url}");

                            foreach (var keyValuePair in productLinkTitleDict)
                            {
                                try
                                {
                                    var productHtmlContent = await _crawlerManager.getLoadedPageContent(keyValuePair.Key);
                                    try
                                    {
                                        await _storageManager.UploadBlob(new Uri(url).Host, keyValuePair.Key, keyValuePair.Value, productHtmlContent);
                                        _logger.LogInformation($"{count}/{totalCount}");
                                        count++;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex.Message);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError($"Trying to fetch next product URL, Failed to fetch {keyValuePair.Key}");
                                }
                            }
                            _logger.LogInformation($"Uploaded {count} product files for URL : {url}");
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
