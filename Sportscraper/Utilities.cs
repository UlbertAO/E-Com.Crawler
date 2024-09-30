
using System.Text.RegularExpressions;

namespace Scraper.Crawler
{
    public class Utilities
    {

        public Boolean isValidUrl(string url)
        {
            try
            {
                new Uri(url);
                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }
        public string getBaseUrl(string url)
        {
            var uri = new Uri(url);
            return $"{uri.Scheme}://{uri.Host}";
        }
        public Boolean containsBaseUrl(string baseUrl, string url)
        {
            return url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase);
        }

        public List<string> getProgressiveUrls(string url)
        {
            var result = new List<string>();
            var uri = new Uri(url);
            var currentUrl = $"{uri.Scheme}://{uri.Host}";
            foreach (var seg in new Uri(url).Segments)
            {
                currentUrl += seg;
                result.Add(currentUrl);
                result.Add(currentUrl.Substring(0, currentUrl.Length - 1));
            }
            return result;
        }
        public string removeQueryPartUrl(string url)
        {
            string removeQueryPattern = @"\?.*$";//?.*{n}
            return Regex.Replace(url, removeQueryPattern, "");
        }
    }
}
