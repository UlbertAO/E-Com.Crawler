using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace E_Com.Crawler
{
    public class CrawlerManager
    {
        private readonly ProductLinkManager _productLinkManager;
        private readonly Utilities _utilities;
        private readonly ILogger _logger;

        public CrawlerManager(ILoggerFactory loggerFactory,ProductLinkManager productLinkManager, Utilities utilities)
        {
            _logger = loggerFactory.CreateLogger<CrawlerManager>();
            _productLinkManager = productLinkManager;
            _utilities = utilities;
        }

        public async Task<Dictionary<string, string>> crawler(string url)
        {
            var uri = new Uri(url);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";

            Dictionary<string, string> linksTitle = new Dictionary<string, string>();

            var htmlContent = await getLoadedPageContent(url);

            // Load HTML into HtmlDocument from HtmlAgilityPack
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Select the body node
            var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
            if (bodyNode == null)
            {
                throw new Exception("No body Found");
            }
            // Parse all anchor tags (<a> tags) inside the <body> tag
            var linkElements = bodyNode.SelectNodes(".//a[@href]");

            if (linkElements == null)
            {
                throw new Exception("No Links Found");
            }

            foreach (var element in linkElements)
            {
                // Get the href & title attribute value(or inner text as a fallback if title is not available)
                var href = element.GetAttributeValue("href", string.Empty);
                var title = element.GetAttributeValue("title", string.Empty).Trim();
                // if no title does that mean its not a product??
                if (string.IsNullOrEmpty(title))
                {
                    title = element.InnerText.Trim();
                }
                if (!string.IsNullOrEmpty(href))
                {
                    // if complete url && contains base url 
                    // if not complete make it complete(add base url)
                    if (!_utilities.isValidUrl(href))
                    {
                        // href= /product/a=>https://www.domain.com/product/a
                        href = baseUrl + href;
                    }
                    if (_utilities.containsBaseUrl(baseUrl, href) && !linksTitle.ContainsKey(href))
                    {
                        linksTitle.Add(href, title);
                    }
                }
            }
            var links = new List<string>(linksTitle.Keys);

            links = _productLinkManager.manager(url, baseUrl, links);

            //assumption title for product will have 2 or more words
            return linksTitle.Where(keyValuePair => links.Contains(keyValuePair.Key) && keyValuePair.Value.Split(' ').Length >= 2).OrderByDescending(keyValuePair => keyValuePair.Key.Length)
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
        }

        public async Task<string> crawlerForContent(string url)
        {
            var uri = new Uri(url);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";
            try
            {

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("accept", "*/*");
                    httpClient.DefaultRequestHeaders.Add("Origin", baseUrl);
                    httpClient.DefaultRequestHeaders.Add("Referer", baseUrl);
                    httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36");
                    httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-GB,en-US;q=0.9,en;q=0.8");
                    httpClient.DefaultRequestHeaders.Add("sec-ch-ua", "Chromium\";v=\"128\", \"Not;A=Brand\";v=\"24\", \"Google Chrome\";v=\"128");
                    httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
                    var response = await httpClient.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    var htmlContent = await response.Content.ReadAsStringAsync();

                    return htmlContent;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogInformation("Received error with httpclient now will be using playwright");
                return await getLoadedPageContent(url);
            }
            catch (Exception ex)
            {
                throw new Exception($"Something went wrong, could not fetch content for : {url}");
            }

        }
        public async Task<string> getLoadedPageContent(string url)
        {
            // Use Playwright to load the page
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();

            // Navigate to the URL and wait for the content to load
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Get the page content (fully rendered HTML including dynamically loaded content)
            var content = await page.ContentAsync();

            // Close the browser
            await browser.CloseAsync();

            return content;
        }

        public async Task<IReadOnlyList<IElementHandle>> getBodyLinkElements(string url)
        {
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();

            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Extract all the links within the <body> tag
            var linkElements = await page.Locator("body a[href]").ElementHandlesAsync();
            await browser.CloseAsync();

            return linkElements;
        }
    }
}
