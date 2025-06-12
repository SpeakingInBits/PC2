using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Azure;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PC2.Models
{
    /// <summary>
    /// Helper class to upload files to Azure Blob Storage.
    /// </summary>
    public class AzureBlobUploader
    {
        private readonly string _containerName;
        private readonly IConfiguration _configuration;

        public AzureBlobUploader(IConfiguration configuration)
        {
            _containerName = configuration["AzureBlob:ContainerName"];
            _configuration = configuration;
        }

        /// <summary>
        /// Uploads an IFormFile to Azure Blob Storage and returns the blob URL.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <param name="blobName">The name to use for the blob in storage.</param>
        /// <returns>The URL of the uploaded blob.</returns>
        public async Task<string> UploadFileAsync(IFormFile file, string blobName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is null or empty.");

            BlobServiceClient blobServiceClient = CreateBlobServiceClient();
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(blobName);

            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, overwrite: true);
            }

            return blobClient.Uri.ToString();
        }

        /// <summary>
        /// Deletes a file (blob) from Azure Blob Storage.
        /// </summary>
        /// <param name="blobName">The name of the blob to delete.</param>
        /// <returns>True if the blob was deleted, false if it did not exist.</returns>
        public async Task<bool> DeleteFileAsync(string blobName)
        {
            // Decode the blob name to match the expected format in Azure Blob Storage
            blobName = Uri.UnescapeDataString(blobName);

            BlobServiceClient blobServiceClient = CreateBlobServiceClient(); 
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();
            return response.Value;
        }

        private BlobServiceClient CreateBlobServiceClient()
        {
#if DEBUG
            return new BlobServiceClient(_configuration["AzureBlob:BlobServiceUri"]);
#else
            return new BlobServiceClient(new Uri(_configuration["AzureBlob:BlobServiceUri"]), new DefaultAzureCredential());
#endif
        }
    }
}
