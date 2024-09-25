using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Text.Json;

namespace E_Com.Crawler
{
    public class CrawlerManager
    {
        private readonly ProductLinkManager _productLinkManager;
        private readonly Utilities _utilities;
        private readonly ILogger _logger;
        private readonly AppSetting _appSetting;

        public CrawlerManager(ILoggerFactory loggerFactory, AppSetting appSetting, ProductLinkManager productLinkManager, Utilities utilities)
        {
            _logger = loggerFactory.CreateLogger<CrawlerManager>();
            _appSetting = appSetting;
            _productLinkManager = productLinkManager;
            _utilities = utilities;
        }

        public async Task<Dictionary<string, string>> crawler(string url, Func<HtmlNode, string, Dictionary<string, string>> parsingStrategy)
        {
            var htmlContent = await getLoadedPageContent(url);
            // Load HTML into HtmlDocument from HtmlAgilityPack
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // Select the body node
            var bodyNode = htmlDoc.DocumentNode.SelectSingleNode("//body");
            if (bodyNode == null)
            {
                throw new Exception("No <body> Found");
            }
            Dictionary<string, string> linksTitle = new Dictionary<string, string>();
            linksTitle = parsingStrategy(bodyNode, url);

            var links = new List<string>(linksTitle.Keys); ;

            links = _productLinkManager.manager(url, _utilities.getBaseUrl(url), links);

            //assumption: if product url have query, when comparing with dictionery remove query part as logic in product link manager for query removal
            return linksTitle.Where(keyValuePair => links.Contains(_utilities.removeQueryPartUrl(keyValuePair.Key))).OrderByDescending(keyValuePair => keyValuePair.Key.Length)
                .ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
        }

        public async Task<object> stratigyAnalyser(string url)
        {
            _logger.LogInformation($"Executing stratigies on url: {url} ");
            var stratigy1dict = await crawler(url, parsingStrategy1);
            var stratigy2dict = await crawler(url, parsingStrategy2);

            return new
            {
                Strategy1Count = stratigy1dict.Count,
                Strategy2Count = stratigy2dict.Count,
                Url = url
            };
        }

        public async Task<string> getLoadedPageContentHttpClient(string url)
        {
            var uri = new Uri(url);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";
            try
            {

                using (var httpClient = new HttpClient())
                {
                    foreach (var header in getReqHeaders(url))
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
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
            try
            {
                // Use Playwright to load the page
                using var playwright = await Playwright.CreateAsync();
                var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
                var context = await browser.NewContextAsync();
                var page = await context.NewPageAsync();

                // Set the required request headers
                await page.SetExtraHTTPHeadersAsync(getReqHeaders(url));

                // Navigate to the URL and wait for the content to load
                await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.Load });

                // Get the page content (fully rendered HTML including dynamically loaded content)
                var content = await page.ContentAsync();

                // Close the browser
                await browser.CloseAsync();

                return content;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Dictionary<string, string> getReqHeaders(string url)
        {
            var uri = new Uri(url);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";
            return (new()
            {
                { "accept", "*/*" },
                { "accept-encoding", "gzip, deflate, br, zstd" },
                { "accept-language", "en-GB,en;q=0.9,en-US;q=0.8" },
                { "sec-ch-ua", $"\"Microsoft Edge\";v=\"129\", \"Not=A?Brand\";v=\"8\", \"Chromium\";v=\"129\"" },
                { "sec-ch-ua-platform", $"\"Windows\"" },
                { "sec-fetch-dest", "document" },
                { "sec-fetch-mode", "navigate" },
                { "user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/129.0.0.0 Safari/537.36 Edg/129.0.0.0" },
            });
        }

        //assumption: all products will have img which will be wraped inside <a>
        //select all <a> elements that have an href attribute and at least one child element that is an < img > need not to be direct
        public Dictionary<string, string> parsingStrategy1(HtmlNode bodyNode, string url)
        {
            var baseUrl = _utilities.getBaseUrl(url);
            Dictionary<string, string> linksTitle = new Dictionary<string, string>();

            var linkElements = bodyNode.SelectNodes(".//a[@href][.//img]");

            if (linkElements == null)
            {
                throw new Exception("No Links Found");
            }

            foreach (var element in linkElements)
            {
                // Get the href & title attribute value(img alt text as a fallback if title is not available)
                var href = element.GetAttributeValue("href", string.Empty);
                var title = element.GetAttributeValue("title", string.Empty).Trim();

                // Find the <img> elements within this <a> tag
                var imgElement = element.SelectSingleNode(".//img");
                if (imgElement != null)
                {
                    // Get the alt attribute value
                    var altText = imgElement.GetAttributeValue("alt", string.Empty);
                    if (string.IsNullOrEmpty(title))
                    {
                        title = altText;
                    }
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
                    // assumption: product name will contain more than 3 words/ grp
                    if (_utilities.containsBaseUrl(baseUrl, href) && !linksTitle.ContainsKey(href) && title.Trim().Split(" ").Length >= _appSetting.ThresholdTitleLength)
                    {
                        linksTitle.Add(href, title);
                    }
                    //if (title.Trim().Split(" ").Length <= 3)
                    //{
                    //    _logger.LogInformation($"Filtered out due to product name threshold => {title}<{href}> ");
                    //}
                }
            }

            return linksTitle;
        }

        //assumption: all products will have name text wraped inside <a> but img will be there for products
        //select all <a> elements that have an href attribute and select all img have alt attribute
        // compare and make dictionary of link:title
        public Dictionary<string, string> parsingStrategy2(HtmlNode bodyNode, string url)
        {
            var baseUrl = _utilities.getBaseUrl(url);
            Dictionary<string, string> linksTitle = new Dictionary<string, string>();
            //link elements
            var linkElements = bodyNode.SelectNodes(".//a[@href]");

            if (linkElements == null)
            {
                throw new Exception("No <a> Found");
            }

            foreach (var element in linkElements)
            {
                // Get the href & title attribute value(or InnerText as a fallback if title is not available)
                var href = element.GetAttributeValue("href", string.Empty);
                var title = element.GetAttributeValue("title", string.Empty).Trim();

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

                    // get all a that have title a{productname}
                    if (_utilities.containsBaseUrl(baseUrl, href) && !linksTitle.ContainsKey(href) && !string.IsNullOrEmpty(title))
                    {
                        linksTitle.Add(href, title);
                    }
                }
            }

            //img elements
            var imgElements = bodyNode.SelectNodes(".//img[@alt]");

            if (imgElements == null)
            {
                throw new Exception("No <img> Found");
            }

            List<string> imgAltText = new List<string>();
            foreach (var element in imgElements)
            {
                // Get the alt assuming it is a product name
                var altTitle = element.GetAttributeValue("alt", string.Empty).Trim();
                if (!string.IsNullOrEmpty(altTitle))
                {
                    imgAltText.Add(altTitle);
                }
            }
            return linksTitle.Where(keyValuePair => imgAltText.Contains(keyValuePair.Value)).ToDictionary(keyValuePair => keyValuePair.Key, keyValuePair => keyValuePair.Value);
        }

    }
}
