using SkiaSharp;

namespace PC2.Services
{
    /// <summary>
    /// Service for image processing operations like resizing using SkiaSharp
    /// </summary>
    public class ImageService
    {
        /// <summary>
        /// Trilinear (mipmapped) sampling. Skia's cubic samplers alias badly when downscaling, mipmaps don't.
        /// </summary>
        private static readonly SKSamplingOptions DownscaleSampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);

        private const int JpegQuality = 85;

        /// <summary>
        /// Formats browsers display that an upload can be stored in as-is
        /// </summary>
        private static readonly Dictionary<SKEncodedImageFormat, (string ContentType, string FileExtension)> WebFormats = new()
        {
            [SKEncodedImageFormat.Jpeg] = ("image/jpeg", ".jpg"),
            [SKEncodedImageFormat.Png] = ("image/png", ".png"),
            [SKEncodedImageFormat.Gif] = ("image/gif", ".gif"),
            [SKEncodedImageFormat.Bmp] = ("image/bmp", ".bmp"),
            [SKEncodedImageFormat.Webp] = ("image/webp", ".webp"),
        };

        private readonly ILogger<ImageService> _logger;

        public ImageService(ILogger<ImageService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Resizes an image to the specified maximum dimensions while maintaining aspect ratio.
        /// Images that already fit are returned unchanged; resized images are saved as PNG if they
        /// have transparency (JPEG would turn it black) and as JPEG otherwise.
        /// </summary>
        /// <param name="imageStream">The input image stream</param>
        /// <param name="maxWidth">Maximum width</param>
        /// <param name="maxHeight">Maximum height</param>
        /// <returns>The image, with the content type and file extension of the format it is actually in</returns>
        public async Task<ResizedImage> ResizeImageAsync(Stream imageStream, int maxWidth = 800, int maxHeight = 600)
        {
            try
            {
                using var inputBuffer = new MemoryStream();
                await imageStream.CopyToAsync(inputBuffer);
                byte[] originalBytes = inputBuffer.ToArray();

                using var data = SKData.CreateCopy(originalBytes);
                using var codec = SKCodec.Create(data)
                    ?? throw new InvalidDataException("Unsupported or corrupt image data.");
                using var decoded = SKBitmap.Decode(codec)
                    ?? throw new InvalidDataException("Unable to decode image data.");

                // Skia doesn't carry EXIF metadata into the re-encoded JPEG, so bake the
                // camera orientation into the pixels or phone photos come out sideways
                using var oriented = ApplyEncodedOrigin(decoded, codec.EncodedOrigin);
                var image = oriented ?? decoded;

                // Calculate new dimensions while maintaining aspect ratio
                (int newWidth, int newHeight) = CalculateResizeDimensions(image.Width, image.Height, maxWidth, maxHeight);

                bool fits = newWidth == image.Width && newHeight == image.Height;

                // If image is already smaller than max dimensions, return original
                if (fits && WebFormats.TryGetValue(codec.EncodedFormat, out var originalFormat))
                {
                    return new ResizedImage(new MemoryStream(originalBytes), originalFormat.ContentType, originalFormat.FileExtension);
                }

                // Resize the image (an image that fits but isn't in a web format is just re-encoded)
                using var resized = fits ? null : image.Resize(image.Info.WithSize(newWidth, newHeight), DownscaleSampling)
                    ?? throw new InvalidOperationException("Unable to resize image.");
                var output = resized ?? image;

                // Use JPEG format with good quality for most cases; PNG keeps transparency
                var format = HasTransparency(output) ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
                var outputStream = new MemoryStream();
                if (!output.Encode(outputStream, format, JpegQuality))
                    throw new InvalidOperationException($"Unable to encode resized image as {format}.");

                outputStream.Position = 0;
                var (contentType, fileExtension) = WebFormats[format];
                return new ResizedImage(outputStream, contentType, fileExtension);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resizing image");
                throw new InvalidOperationException("Failed to resize image", ex);
            }
        }

        /// <summary>
        /// Calculates new dimensions for resizing while maintaining aspect ratio. Minimizes the size to fit within maxWidth and maxHeight.
        /// </summary>
        private static (int width, int height) CalculateResizeDimensions(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            // If image is already within bounds, return original size
            if (originalWidth <= maxWidth && originalHeight <= maxHeight)
                return (originalWidth, originalHeight);

            // Calculate scaling ratios
            double widthRatio = (double)maxWidth / originalWidth;
            double heightRatio = (double)maxHeight / originalHeight;

            // Use the smaller ratio to ensure image fits within both dimensions
            double ratio = Math.Min(widthRatio, heightRatio);

            int newWidth = (int)(originalWidth * ratio);
            int newHeight = (int)(originalHeight * ratio);

            return (newWidth, newHeight);
        }

        /// <summary>
        /// Returns a copy of the bitmap rotated/flipped upright according to its EXIF orientation,
        /// or null if it is already upright.
        /// </summary>
        private static SKBitmap? ApplyEncodedOrigin(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            if (origin == SKEncodedOrigin.TopLeft)
                return null;

            int w = bitmap.Width;
            int h = bitmap.Height;

            // Maps stored pixel (x, y) to its upright position: x' = ScaleX*x + SkewX*y + TransX, y' = SkewY*x + ScaleY*y + TransY
            SKMatrix transform = origin switch
            {
                SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),     // mirror horizontally
                SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1), // rotate 180°
                SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),   // mirror vertically
                SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),       // transpose
                SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),     // rotate 90° clockwise
                SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1), // transverse
                SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),   // rotate 90° counter-clockwise
                _ => SKMatrix.Identity
            };

            bool swapsDimensions = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            var info = swapsDimensions ? bitmap.Info.WithSize(h, w) : bitmap.Info;

            var upright = new SKBitmap(info);
            using var canvas = new SKCanvas(upright);
            // New bitmaps aren't zeroed, and transparent pixels would let that garbage show through
            canvas.Clear(SKColors.Transparent);
            canvas.SetMatrix(transform);
            // Every orientation maps pixels 1:1, so nearest-neighbor copies them exactly
            canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
            return upright;
        }

        /// <summary>
        /// True if any pixel is less than fully opaque. Checks the pixels themselves because
        /// many PNGs have an alpha channel that is never actually used.
        /// </summary>
        private static bool HasTransparency(SKBitmap bitmap)
        {
            if (bitmap.AlphaType == SKAlphaType.Opaque)
                return false;

            using var pixmap = bitmap.PeekPixels();
            return !pixmap.ComputeIsOpaque();
        }

        /// <summary>
        /// Validates if the uploaded file is a valid image
        /// </summary>
        public static bool IsValidImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            var allowedMimeTypes = new[]
            {
                "image/jpeg",
                "image/jpg", 
                "image/png",
                "image/gif",
                "image/bmp",
                "image/webp"
            };

            return allowedMimeTypes.Contains(file.ContentType?.ToLower());
        }

        /// <summary>
        /// Gets a safe filename for uploaded images
        /// </summary>
        /// <param name="personId">The person the photo belongs to</param>
        /// <param name="fileExtension">The extension of the format the image is stored in, e.g. <see cref="ResizedImage.FileExtension"/></param>
        public static string GetSafeImageFileName(int personId, string fileExtension)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"person_{personId}_{timestamp}{fileExtension}";
        }
    }
}