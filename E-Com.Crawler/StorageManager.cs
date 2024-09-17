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
                _logger.LogError("No AzureWebJobsStorage connection string found in environment variables.");
                return;
            }
            _blobServiceClient = new BlobServiceClient(connectionString);
        }
        public async Task CreateContainer()
        {
            var containerName = Environment.GetEnvironmentVariable("ContainerName");
            if (containerName == null)
            {
                _logger.LogError("No containerName found in environment variables.");
                return;
            }
            _containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await _containerClient.CreateIfNotExistsAsync();
        }
        public async Task UploadBlob(string productName, string htmlContent)
        {
            var currentDate = DateTime.Now.ToString("yyyy/MM/dd");
            var blobName = $"{currentDate}/{productName}.html";

            BlobClient blobClient = _containerClient.GetBlobClient(blobName);


            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(htmlContent)))
            {
                await blobClient.UploadAsync(stream, overwrite: true);
                _logger.LogInformation($"Uploaded HTML to blob storage.");
            }
        }

    }
}
