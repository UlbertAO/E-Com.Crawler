using Microsoft.Playwright;
using System.Text.RegularExpressions;


namespace E_Com.Crawler
{
    public class CrawlerManager
    {
        private readonly ProductLinkManager _productLinkManager;
        private readonly Utilities _utilities;


        public CrawlerManager(ProductLinkManager productLinkManager, Utilities utilities)
        {
            _productLinkManager = productLinkManager;
            _utilities = utilities;


        }

        public async Task<List<string>> getAllLinksFromBody(string url)
        {
            var uri = new Uri(url);
            var baseUrl = $"{uri.Scheme}://{uri.Host}";

            var links = new List<string>();

            // Playwright to load the entire page, including all dynamically loaded content
            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
            var page = await browser.NewPageAsync();

            // Navigate to the URL and wait for the content to load
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

            // Extract all the links within the <body> tag
            var linkElements = await page.Locator("body a[href]").ElementHandlesAsync();
            if (linkElements == null)
            {
                throw new Exception("No Links Found");
            }
            foreach (var element in linkElements)
            {
                // Get the href attribute of each link
                var href = await element.GetAttributeAsync("href");
                //// Get the title attribute (or inner text as a fallback if title is not available)
                //var title = await element.GetAttributeAsync("title");

                //// If the title attribute is not present, fall back to using the inner text
                //if (string.IsNullOrEmpty(title))
                //{
                //    title = await element.InnerTextAsync();
                //}

                if (!string.IsNullOrEmpty(href))
                {
                    if (!_utilities.isValidUrl(href))
                    {
                        // href= /product/a=>https://www.domain.com/product/a
                        href = baseUrl + href;
                    }
                    if (_utilities.containsBaseUrl(baseUrl, href))
                    {
                        links.Add(href);
                    }
                    // if complete url && contains base url 
                    // if not complete make it complete(add base url)
                }
            }

            //remove url present in links not needed upto 2 places of wild characters
            string urlPattern = @$"{baseUrl}..";
            links.RemoveAll(link => !Regex.IsMatch(link, urlPattern));

            var productUrlContainsSegments = Boolean.Parse(Environment.GetEnvironmentVariable("ProductUrlContainsSegments"));
            if (productUrlContainsSegments)
            {
                // url:provided in environment variable
                // links: found in page
                links = _productLinkManager.getProductUrlContainsSegments(url, links);
            }

            //now we have all links filter out only child links
            links = _productLinkManager.getChildUrls(links);

            // Close the browser
            await browser.CloseAsync();

            return links;
        }





    }
}
