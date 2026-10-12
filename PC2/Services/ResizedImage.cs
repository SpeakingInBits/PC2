namespace PC2.Services;

/// <summary>
/// An image from <see cref="ImageService.ResizeImageAsync"/>, with the content type and
/// file extension of the format its bytes are actually in.
/// </summary>
public sealed record ResizedImage(MemoryStream Content, string ContentType, string FileExtension) : IDisposable
{
    public void Dispose() => Content.Dispose();
}
