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

        private readonly ILogger<ImageService> _logger;

        public ImageService(ILogger<ImageService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Resizes an image to the specified maximum dimensions while maintaining aspect ratio
        /// </summary>
        /// <param name="imageStream">The input image stream</param>
        /// <param name="maxWidth">Maximum width</param>
        /// <param name="maxHeight">Maximum height</param>
        /// <returns>Resized image as a memory stream</returns>
        public async Task<MemoryStream> ResizeImageAsync(Stream imageStream, int maxWidth = 800, int maxHeight = 600)
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

                // If image is already smaller than max dimensions, return original
                if (newWidth == image.Width && newHeight == image.Height)
                {
                    return new MemoryStream(originalBytes);
                }

                // Resize the image
                using var resized = image.Resize(image.Info.WithSize(newWidth, newHeight), DownscaleSampling)
                    ?? throw new InvalidOperationException("Unable to resize image.");

                // Use JPEG format with good quality for most cases
                var outputStream = new MemoryStream();
                if (!resized.Encode(outputStream, SKEncodedImageFormat.Jpeg, JpegQuality))
                    throw new InvalidOperationException("Unable to encode resized image as JPEG.");

                outputStream.Position = 0;
                return outputStream;
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
            canvas.SetMatrix(transform);
            // Every orientation maps pixels 1:1, so nearest-neighbor copies them exactly
            canvas.DrawBitmap(bitmap, 0, 0, new SKSamplingOptions(SKFilterMode.Nearest));
            return upright;
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
        public static string GetSafeImageFileName(string originalFileName, int personId)
        {
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"person_{personId}_{timestamp}{extension}";
        }
    }
}