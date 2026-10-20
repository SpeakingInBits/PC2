using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PC2.Models;

namespace PC2Tests.Models;

/// <summary>
/// A stand-in for Azure Blob Storage that keeps blobs in memory, so code that uses
/// <see cref="AzureBlobUploader"/> can be tested without Azurite. It answers just the
/// requests the uploader makes: creating the container, uploading a blob and deleting one.
/// </summary>
internal sealed class FakeBlobStorage : IAsyncDisposable
{
    private const string AccountName = "devstoreaccount1";
    private const string ContainerName = "files";

    private readonly WebApplication _app;
    private readonly ConcurrentDictionary<string, byte[]> _blobs = new();

    private FakeBlobStorage(WebApplication app)
    {
        _app = app;
    }

    /// <summary>
    /// Makes uploads fail with 403 Forbidden, which the Azure SDK doesn't retry
    /// </summary>
    public bool FailUploads { get; set; }

    /// <summary>
    /// Makes deletes fail with 403 Forbidden, which the Azure SDK doesn't retry
    /// </summary>
    public bool FailDeletes { get; set; }

    /// <summary>
    /// The names of the blobs currently stored
    /// </summary>
    public ICollection<string> BlobNames => _blobs.Keys;

    private string BlobEndpoint => $"{_app.Urls.Single()}/{AccountName}";

    public static async Task<FakeBlobStorage> StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var app = builder.Build();
        var storage = new FakeBlobStorage(app);

        app.MapPut("/{account}/{container}", () => Results.StatusCode(StatusCodes.Status201Created));
        app.MapPut("/{account}/{container}/{blobName}", storage.UploadAsync);
        app.MapDelete("/{account}/{container}/{blobName}", storage.Delete);

        await app.StartAsync();
        return storage;
    }

    /// <summary>
    /// Creates an uploader that stores its files here
    /// </summary>
    public AzureBlobUploader CreateUploader()
    {
#if !DEBUG
        // Release builds of AzureBlobUploader sign in to a real storage account instead of using a connection string
        Assert.Inconclusive("FakeBlobStorage only works with Debug builds.");
#endif
        // Signatures aren't checked, so any key will do
        string accountKey = Convert.ToBase64String(new byte[64]);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AzureBlob:ContainerName"] = ContainerName,
                ["AzureBlob:BlobServiceUri"] = $"DefaultEndpointsProtocol=http;AccountName={AccountName};AccountKey={accountKey};BlobEndpoint={BlobEndpoint};"
            })
            .Build();
        return new AzureBlobUploader(configuration);
    }

    /// <summary>
    /// Stores a blob, as if it had been uploaded earlier
    /// </summary>
    /// <returns>The blob's URL</returns>
    public string AddBlob(string blobName, byte[] content)
    {
        _blobs[blobName] = content;
        return GetUrl(blobName);
    }

    /// <summary>
    /// Gets the URL a blob is served from, the same as the URL <see cref="AzureBlobUploader.UploadFileAsync"/> returns
    /// </summary>
    public string GetUrl(string blobName) => $"{BlobEndpoint}/{ContainerName}/{blobName}";

    private async Task<IResult> UploadAsync(string blobName, HttpRequest request)
    {
        if (FailUploads) return Forbidden();

        using var content = new MemoryStream();
        await request.Body.CopyToAsync(content);
        _blobs[blobName] = content.ToArray();
        return Results.StatusCode(StatusCodes.Status201Created);
    }

    private IResult Delete(string blobName, HttpResponse response)
    {
        if (FailDeletes) return Forbidden();

        if (_blobs.TryRemove(blobName, out _)) return Results.StatusCode(StatusCodes.Status202Accepted);

        response.Headers["x-ms-error-code"] = "BlobNotFound";
        return Results.NotFound();
    }

    private static IResult Forbidden() => Results.StatusCode(StatusCodes.Status403Forbidden);

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
