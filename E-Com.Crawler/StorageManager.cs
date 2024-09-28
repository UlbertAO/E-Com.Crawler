using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace E_Com.Crawler
{
    public class StorageManager
    {
        private readonly ILogger _logger;
        private readonly AppSetting _appSetting;
        private readonly BlobServiceClient _blobServiceClient;
        public BlobContainerClient _containerClient;
        public StorageManager(ILoggerFactory loggerFactory, AppSetting appSetting)
        {
            _logger = loggerFactory.CreateLogger<StorageManager>();
            _appSetting = appSetting;

            _blobServiceClient = new BlobServiceClient(appSetting.AzureWebJobsStorage);
        }
        public async Task CreateContainer()
        {
            _containerClient = _blobServiceClient.GetBlobContainerClient(_appSetting.ContainerName);
            await _containerClient.CreateIfNotExistsAsync();
        }
        public async Task UploadBlob(string hostName, string productUrl, string productName, string htmlContent)
        {
            try
            {
                var productUrlSplit = productUrl.Split('/');
                var fileName = !string.IsNullOrEmpty(productName) ? $"{productName.Replace("/", "_")}.html" : productUrlSplit[productUrlSplit.Length - 1];
                var currentDate = DateTime.Now.ToString("yyyy-MM-dd");
                var blobName = $"{hostName}/{currentDate}/{fileName}";

                BlobClient blobClient = _containerClient.GetBlobClient(blobName);

                using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlContent)))
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Something went wrong, could not upload content for : {productUrl}");
            }
        }

    }
}
