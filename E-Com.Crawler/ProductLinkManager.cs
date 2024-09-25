using System.Text.RegularExpressions;

namespace E_Com.Crawler
{
    public class ProductLinkManager
    {
        private readonly Utilities _utilities;

        public ProductLinkManager(Utilities utilities)
        {
            _utilities = utilities;

        }
        public List<string> manager(string url, string baseUrl, List<string> productLinks)
        {
            //assumption: query part does not hold value to differentiate product
            // sanity: remove all query part from the url in list 
            productLinks = productLinks.Select(link => _utilities.removeQueryPartUrl(link)).ToList();

            // assumption: after baseurl & url are query sanitized url path without / . is not a product link
            string urlPattern1 = @$"{Regex.Escape(baseUrl)}" + @".[^/.]*$";
            productLinks.RemoveAll(link => Regex.IsMatch(link, urlPattern1));
            string urlPattern2 = @$"{Regex.Escape(_utilities.removeQueryPartUrl(url))}" + @".[^/.]*$";
            productLinks.RemoveAll(link => Regex.IsMatch(link, urlPattern2));// needed??

            // assumption urls followed by base url having words seperated by / is not a product url
            string urlPattern3 = @$"{Regex.Escape(baseUrl)}" + @"(\/[a-zA-Z]+)+$"; // ^(\/[a-zA-Z]+)+$
            productLinks.RemoveAll(link => Regex.IsMatch(link, urlPattern3));


            // assumption: final product link will have atleast 2 segment 
            //https://www.domain.com/category/product
            //https://www.domain.com/product IGNORING
            productLinks.RemoveAll(link => new Uri(link).LocalPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Length < 2);

            // THIS WILL NOT WORK CUZ IN MANY CASES PRODUCT LINK AND SEG HAVE NO CONNECTION but keeping this for future ref
            //2. keep only those url having segments of url provided
            // this will remove products if product url dont have segments present actually
            //var productUrlContainsSegments = Boolean.Parse(Environment.GetEnvironmentVariable("ProductUrlContainsSegments"));
            //if (productUrlContainsSegments)
            //{
            //    // url:provided in environment variable
            //    // links: found in page
            //    productLinks = getProductUrlContainsSegments(url, productLinks);
            //}

            //3.now we have all links filter out only child links
            productLinks = getChildUrls(productLinks);

            return productLinks;
        }

        //url: https://www.domain.com/pro/a/b
        // link: https://www.domain.com/pro/a/b/c/d/e/f=>it contains some segment of url so might be product link
        private List<string> getProductUrlContainsSegments(string url, List<string> productLinks)
        {
            var segments = new Uri(url).LocalPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            // Remove strings whose length is less than 2
            segments.RemoveAll(s => s.Length < 3);
            var possibleProductLinks = new List<string>();
            foreach (var pl in productLinks)
            {
                if (segments.Any(seg => pl.Contains(seg, StringComparison.OrdinalIgnoreCase)))
                {
                    possibleProductLinks.Add(pl);
                }
            }
            return possibleProductLinks;
        }


        /*
         https://www.domain.com/products/
         https://www.domain.com/products/a
         https://www.domain.com/products/a/b
         https://www.domain.com/products/a/b/c
         https://www.domain.com/products/a/b/c/d=> only need this link
         */
        //basically getonly child logic
        private List<string> getChildUrls(List<string> links)
        {
            //unique 1st then sort
            //descending order of the length of sting in list and then sort in ascending order of string characters
            var sortedList = links.Distinct().OrderByDescending(link => link.Length).ThenBy(link => link).ToList();
            var possibleProductLinks = new List<string>();
            var itemsToRemove = new List<string>();
            foreach (var url in sortedList)
            {
                if (itemsToRemove.Contains(url))
                {
                    continue;
                }
                possibleProductLinks.Add(url);

                itemsToRemove.AddRange(_utilities.getProgressiveUrls(url));//refactor required
                itemsToRemove = itemsToRemove.Distinct().ToList();

            }
            return possibleProductLinks;
        }
    }
}
