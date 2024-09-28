This project creates Azure functions that download all the product pages from the websites below and saves the HTMLs into blob storage.

https://www.adidas.co.uk/football?grid=true
https://arsenaldirect.arsenal.com/
https://shop.avfc.co.uk/en/aston-villa/o-43089351+t-53091948+z-989-4025465990?pageNumber=1&pageSize=72&sortOption=TopSellers&cur=GBP
https://shop.brentfordfc.com/Site-Map.php
https://shop.brightonandhovealbion.com/kit/home-kit/
https://castore.com/collections/outlet
https://store.celticfc.com/collections
https://www4.chelseamegastore.com/en/chelsea-men/t-43110668+ga-34+z-915271-809855383
https://evertondirect.evertonfc.com/en/everton/t-19321265+z-9197965-3477742524?pageSize=96&cur=GBP&sortOption=TopSellers&vap=1
https://www.fanatics.co.uk/en/premier-league/o-10089362+z-9896720-1139193336?pageSize=96&pageNumber=1&sortOption=TopSellers&vap=1

## Local setup

In order to run this locally you would need 'local.settings.json'

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "IsAnalysisMode": false,
    "ContainerName": "html-data",
    "ParsingStrategy1EcomUrls": "https://www.example1.com/category1/;https://www.example2.com/category2/",
    "ParsingStrategy2EcomUrls": "https://www.example3.com/category1/;https://www.example4.com/category2/",
    "ThresholdTitleLength": "3",
    "ProductUrlContainsSegments": false
  }
}
```

execute following for headless chromium browser

```bash
./bin/Debug/netX/playwright.ps1 install
```

# Strategies

These strategies are based on some assumptions that needs to be fulfilled in order to classify a specific URL a Product URL.
Based on the strategy place them in respective EcomUrls environment variable in local.settings.json

On sites links can be of 2 types

- complete URL `https://www.domain.com/category/product`
- relative URL `/category/product`
  Complete URL is fine, but in case of relative URL, concatinate host & relative URL so at the end we will get complete URL anyways.

Parse the content using strategies and provide link:title key value pair Dictionary which will be used to download & save in blob storage and in naming file

## Strategy 1

- All products will have <img> which will be wrapped within <a>
  - href and title will be fetched from <a>
  - <img> need not to be direct child of <a>
  - if title in not available in <a> pick <img> alt text
- Actual product name will have more then 1 word init, by this we can filter many unwanted links present on hte page
  - eg: /login,/review,/club or anything
  - use `ThresholdTitleLength` to set the cap
  - THIS MAY FILTER OUT ACTUAL PRODUCT AS WELL IF THIS IS NOT SUITED GO FOR OTHER STRATEGIES
- Dictionary is created using above assumtion

NOTE:Since all product img need not to be wrapped within a so we have another approach

## Strategy 2

- All products will have name which will be wrapped within <a> & image of a product will also be there but may be outsite <a>.
- Get href and title(fallback to inner text) from all <a> , anyhow title should be present.
  - Dictionary is created using above parsed content
- Get alt from all <img> and create list.
- Filter dictionary to have only key:values where value/title have matching in list of alt

## Common Parsing Strategy on top of above strategies

- create list of all URLs from dictionary
- if link have query part then remove query part of the URL
- From list remove URLs that have
  - no `/` after base URL
    - eg: `https://www.domain.com/"very_long_category"`
  - no `/` after url provided(query part removed)
    - eg: `https://www.domain.com/category/"may_not_be_a_product"`
  - only a words seperated by `/` after base URL
    - eg: `https://www.domain.com/"register/gift"`
- Filter out only child links
  - child links are those links that have the longest path among similar group of links
- Filter dictionary key:value where key/URL(after removing query part) have maticing url in list or URLs.
