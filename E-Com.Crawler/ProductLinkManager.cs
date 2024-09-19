namespace E_Com.Crawler
{
    public class ProductLinkManager
    {
        private readonly Utilities _utilities;

        public ProductLinkManager(Utilities utilities)
        {
            _utilities = utilities;

        }

        //url: https://www.domain.com/pro/a/b
        // link: https://www.domain.com/pro/a/b/c/d/e/f=>it contains some segment of url so might be product link
        public List<string> getProductUrlContainsSegments(string url, List<string> productLinks)
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
        public List<string> getChildUrls(List<string> links)
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

                itemsToRemove.AddRange(_utilities.getProgressiveUrls(url));
                itemsToRemove = itemsToRemove.Distinct().ToList();

            }
            return possibleProductLinks;
        }
    }
}
