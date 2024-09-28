using System;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
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
        private readonly AppSetting _appSetting;
        [Function("ManualTriggerCrawler")]
        public async Task<HttpResponseData> ManualTrigger(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Manual HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            await response.WriteStringAsync("Crawler process completed. Check logs for details.");

            return response;
        }
        
        public CrawlerCron(IConfiguration configuration, ILoggerFactory loggerFactory, AppSetting appSetting, StorageManager storageManager, CrawlerManager crawlerManager, Utilities utilities)
        {
            _logger = loggerFactory.CreateLogger<CrawlerCron>();
            _logger.LogInformation("CrawlerCron instance created at: {time}", DateTime.UtcNow);
            _configuration = configuration;
            _appSetting = appSetting;
            _storageManager = storageManager;
            _crawlerManager = crawlerManager;
            _utilities = utilities;
        }
        
        [Function("CrawlerCron")]
        public async Task Run([TimerTrigger("*/5 * * * * *", RunOnStartup = true)] TimerInfo timer)
        {
            _logger.LogInformation($"Crawler Timer trigger function execution started at: {DateTime.Now}");
            try
            {
                await _storageManager.CreateContainer();

                var parsingStrategy1EcomUrls = _appSetting.ParsingStrategy1EcomUrls;
                var parsingStrategy2EcomUrls = _appSetting.ParsingStrategy2EcomUrls;

                List<string> urls = new List<string>();
                urls.AddRange(parsingStrategy1EcomUrls);
                urls.AddRange(parsingStrategy2EcomUrls);

                List<object> analysisList = new List<object>();

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
                                        string domain = uri.Host.StartsWith("www.") 
                                            ? uri.Host.Substring(4).Split('.')[0]  // Remove "www." and take what's before the first dot
                                            : uri.Host.Split('.')[0];  // Otherwise, just take what's before the first dot

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
                if (_appSetting.IsAnalysisMode)
                {
                    string jsonString = JsonSerializer.Serialize(analysisList, new JsonSerializerOptions { WriteIndented = true });
                    // Log the JSON string
                    _logger.LogInformation(jsonString);
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
