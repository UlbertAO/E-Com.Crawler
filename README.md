# E-Com.Crawler 

This project creates Azure functions that download all the product pages from the websites below and saves the HTMLs into blob storage.

(Adidas)[https://www.adidas.co.uk/football?grid=true]
(Arsenal)[https://arsenaldirect.arsenal.com/]
(Avfc)[https://shop.avfc.co.uk/en/aston-villa/o-43089351+t-53091948+z-989-4025465990?pageNumber=1&pageSize=72&sortOption=TopSellers&cur=GBP]
(Brentfordfc)[https://shop.brentfordfc.com/Site-Map.php]
(Brightonandhovealbion)[https://shop.brightonandhovealbion.com/kit/home-kit/]
(Castore)[https://castore.com/collections/outlet]
(Celticfc)[https://store.celticfc.com/collections]
(Chelseamegastore)[https://www4.chelseamegastore.com/en/chelsea-men/t-43110668+ga-34+z-915271-809855383]
(Evertonfc)[https://evertondirect.evertonfc.com/en/everton/t-19321265+z-9197965-3477742524?pageSize=96&cur=GBP&sortOption=TopSellers&vap=1]
(Fanatics)[https://www.fanatics.co.uk/en/premier-league/o-10089362+z-9896720-1139193336?pageSize=96&pageNumber=1&sortOption=TopSellers&vap=1]

## Local setup
In order to run this locally you would need 'local.settings.json'

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ContainerName": "html-data",
    //"EcomUrls": "https://www.example1.com/category1/;https://www.example2.com/category2/,
    "ThresholdTitleLength": 3,
    "ProductUrlContainsSegments": true
  }
}
```

Execute following for headless chromium browser

```bash
./bin/Debug/netX/playwright.ps1 install
```

## How it works

The TimerTrigger makes it incredibly easy to have your functions executed on a schedule. 
This sample demonstrates a simple use case of calling your function every 5 minutes.

For a `TimerTrigger` to work, you provide a schedule in the form of a [cron expression](https://en.wikipedia.org/wiki/Cron#CRON_expression). 
A cron expression is a string with 6 separate expressions which represent a given schedule via patterns.
The pattern we use to represent every 5 minutes is `0 */5 * * * *`. 
This, in plain text, means: "When seconds is equal to 0, minutes is divisible by 5, for any hour, day of the month, month, day of the week, or year".

<!-- ## (Learn More)[https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-timer?tabs=python-v2%2Cisolated-process%2Cnodejs-v4&pivots=programming-language-csharp] -->
