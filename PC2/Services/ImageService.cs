using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Bmp;

namespace PC2.Services
{
    /// <summary>
    /// Service for image processing operations like resizing using ImageSharp
    /// </summary>
    public class ImageService
    {
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
                using var image = await Image.LoadAsync(imageStream);
                
                // Calculate new dimensions while maintaining aspect ratio
                (int newWidth, int newHeight) = CalculateResizeDimensions(image.Width, image.Height, maxWidth, maxHeight);
                
                // If image is already smaller than max dimensions, return original
                if (newWidth == image.Width && newHeight == image.Height)
                {
                    var originalStream = new MemoryStream();
                    imageStream.Position = 0;
                    await imageStream.CopyToAsync(originalStream);
                    originalStream.Position = 0;
                    return originalStream;
                }

                // Resize the image
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(newWidth, newHeight),
                    Mode = ResizeMode.Max, // Ensure it fits within max dimensions
                    Sampler = KnownResamplers.Lanczos3 // High quality resampling (more CPU intensive)
                }));

                // Save to memory stream
                var outputStream = new MemoryStream();
                
                // Use JPEG format with good quality for most cases
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder
                {
                    Quality = 85
                });

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
        public static string GetSafeImageFileName(string originalFileName, int staffId)
        {
            var extension = Path.GetExtension(originalFileName);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            return $"staff_{staffId}_{timestamp}{extension}";
        }
    }
}