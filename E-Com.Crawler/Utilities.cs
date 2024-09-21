
using System.Text.RegularExpressions;

namespace E_Com.Crawler
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
        public Boolean containsBaseUrl(string baseUrl, string url)
        {
            return url.StartsWith(baseUrl, StringComparison.OrdinalIgnoreCase);
        }

        /*
         "https://www.domain.com/productsxxzz/a/b/c/d/e/f/g/h",
        output:
        "https://www.domain.com/productsxxzz/a/b/c/d/e/f/g"
        "https://www.domain.com/productsxxzz/a/b/c/d/e/f"
        "https://www.domain.com/productsxxzz/a/b/c/d/e"
        "https://www.domain.com/productsxxzz/a/b/c/d"
        "https://www.domain.com/productsxxzz/a/b/c"
        "https://www.domain.com/productsxxzz/a/b"
        "https://www.domain.com/productsxxzz/a"
        "https://www.domain.com/productsxxzz"
        "https://www.domain.com"=> until url becomes the baseurl
         */
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
