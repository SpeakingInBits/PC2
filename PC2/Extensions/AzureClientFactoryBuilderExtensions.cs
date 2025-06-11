using Azure.Core.Extensions;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Microsoft.Extensions.Azure;

internal static class AzureClientFactoryBuilderExtensions
{
    public static IAzureClientBuilder<BlobServiceClient, BlobClientOptions> AddBlobServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi = true)
    {
        if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri? serviceUri))
        {
            return builder.AddBlobServiceClient(serviceUri);
        }
        else
        {
            return BlobClientBuilderExtensions.AddBlobServiceClient(builder, serviceUriOrConnectionString);
        }
    }

    public static IAzureClientBuilder<QueueServiceClient, QueueClientOptions> AddQueueServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi = true)
    {
        if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri? serviceUri))
        {
            return builder.AddQueueServiceClient(serviceUri);
        }
        else
        {
            return QueueClientBuilderExtensions.AddQueueServiceClient(builder, serviceUriOrConnectionString);
        }
    }

    public static IAzureClientBuilder<TableServiceClient, TableClientOptions> AddTableServiceClient(this AzureClientFactoryBuilder builder, string serviceUriOrConnectionString, bool preferMsi = true)
    {
        if (preferMsi && Uri.TryCreate(serviceUriOrConnectionString, UriKind.Absolute, out Uri? serviceUri))
        {
            return builder.AddTableServiceClient(serviceUri);
        }
        else
        {
            return TableClientBuilderExtensions.AddTableServiceClient(builder, serviceUriOrConnectionString);
        }
    }
}
