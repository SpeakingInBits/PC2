using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
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
        private readonly string _connectionString;
        private readonly string _containerName;

        public AzureBlobUploader(string connectionString, string containerName)
        {
            _connectionString = connectionString;
            _containerName = containerName;
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

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
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
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DeleteIfExistsAsync();
            return response.Value;
        }
    }
}
