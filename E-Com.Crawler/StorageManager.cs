using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace E_Com.Crawler
{
    public class StorageManager
    {
        private readonly ILogger _logger;
        private BlobServiceClient _blobServiceClient;
        public BlobContainerClient _containerClient;
        public StorageManager(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<StorageManager>();

            var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            if (connectionString == null)
            {
                var msg = "No AzureWebJobsStorage connection string found in environment variables.";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
        public async Task CreateContainer()
        {
            var containerName = Environment.GetEnvironmentVariable("ContainerName");
            if (containerName == null)
            {
                var msg = "No containerName found in environment variables. So container cannot be created";
                _logger.LogError(msg);
                throw new Exception(msg);
            }
            _containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await _containerClient.CreateIfNotExistsAsync();
        }
        public async Task UploadBlob(string hostName, string productUrl, string productName, string htmlContent)
        {
            try
            {
                var productUrlSplit = productUrl.Split('/');
                var fileName= !string.IsNullOrEmpty(productName) ? $"{productName.Replace("/", "_")}.html":productUrlSplit[productUrlSplit.Length - 1];
                var currentDate = DateTime.Now.ToString("yyyy/MM/dd");
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
