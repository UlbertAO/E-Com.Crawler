using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Scraper.Crawler
{
    public class SportScraper
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly StorageManager _storageManager;
        private readonly CrawlerManager _crawlerManager;
        private readonly Utilities _utilities;
        private readonly AppSetting _appSetting;
        
        public SportScraper(IConfiguration configuration, ILoggerFactory loggerFactory, AppSetting appSetting, StorageManager storageManager, CrawlerManager crawlerManager, Utilities utilities)
        {
            _logger = loggerFactory.CreateLogger<SportScraper>();
            _logger.LogInformation("CrawlerCron instance created at: {time}", DateTime.UtcNow);
            _configuration = configuration;
            _appSetting = appSetting;
            _storageManager = storageManager;
            _crawlerManager = crawlerManager;
            _utilities = utilities;
        }
        
        [Function("SportScraper")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation($"Crawler HTTP trigger function execution started at: {DateTime.Now}");
            List<object> analysisList = new List<object>();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            try
            {
            await _storageManager.CreateContainer();

            var parsingStrategy1EcomUrls = _appSetting.ParsingStrategy1EcomUrls;
            var parsingStrategy2EcomUrls = _appSetting.ParsingStrategy2EcomUrls;

            List<string> urls = new List<string>();
            urls.AddRange(parsingStrategy1EcomUrls);
            urls.AddRange(parsingStrategy2EcomUrls);

            foreach (var url in urls)
            {
                try
                {
                if (_utilities.isValidUrl(url))
                {
                    if (_appSetting.IsAnalysisMode)
                    {
                    analysisList.Add(await _crawlerManager.stratigyAnalyser(url));
                    }
                    else
                    {
                    Dictionary<string, string> productLinkTitleDict = new Dictionary<string, string>();
                    if (parsingStrategy1EcomUrls.Contains(url))
                    {
                        productLinkTitleDict = await _crawlerManager.crawler(url, _crawlerManager.parsingStrategy1);
                        _logger.LogInformation($"Found {productLinkTitleDict.Count} product URLs for {url}");
                    }
                    if (parsingStrategy2EcomUrls.Contains(url))
                    {
                        productLinkTitleDict = await _crawlerManager.crawler(url, _crawlerManager.parsingStrategy2);
                        _logger.LogInformation($"Found {productLinkTitleDict.Count} product URLs for {url}");
                    }
                    var count = 1;
                    var totalCount = productLinkTitleDict.Count;

                    _logger.LogInformation($"Uploading product HTML files to blob for URL: {url}");

                    foreach (var keyValuePair in productLinkTitleDict)
                    {
                        try
                        {
                        var productHtmlContent = await _crawlerManager.getLoadedPageContentScrapingAnt(keyValuePair.Key);
                        try
                        {
                            Uri uri = new Uri(url);

                            // Remove "www." if it exists, and then take the part before the first "."
                            string domain;
                            if (uri.Host.StartsWith("www."))
                            {
                            domain = uri.Host.Substring(4).Split('.')[0];  // Remove "www." and take what's before the first dot
                            }
                            else if (uri.Host.StartsWith("shop."))
                            {
                            domain = uri.Host.Substring(5).Split('.')[0];  // Remove "shop." and take what's before the first dot
                            }
                            else
                            {
                            domain = uri.Host.Split('.')[0];  // Otherwise, just take what's before the first dot
                            }

                            // Use domain in the UploadBlob call
                            await _storageManager.UploadBlob(domain, keyValuePair.Key, keyValuePair.Value, productHtmlContent);

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

            await response.WriteStringAsync("Crawler process completed. Check logs for details.");
            }
            catch (Exception ex)
            {
            _logger.LogError(ex.Message);
            await response.WriteStringAsync("Crawler process encountered an error. Check logs for details.");
            }
            finally
            {
            if (_appSetting.IsAnalysisMode)
            {
                string jsonString = JsonSerializer.Serialize(analysisList, new JsonSerializerOptions { WriteIndented = true });
                // Log the JSON string
                _logger.LogInformation(jsonString);
            }

            _logger.LogInformation($"Crawler HTTP trigger function execution ended at: {DateTime.Now}");
            }

            return response;
        }
    }
}
    
