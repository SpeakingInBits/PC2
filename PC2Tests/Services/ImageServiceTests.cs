using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace PC2.Services.Tests;

[TestClass]
public class ImageServiceTests
{
    // 1x1 GIF; Skia can't encode GIFs, so use a canned one
    private static readonly byte[] TinyGif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

    private readonly ImageService _service = new(NullLogger<ImageService>.Instance);

    [TestMethod]
    [DataRow(SKEncodedImageFormat.Jpeg)]
    [DataRow(SKEncodedImageFormat.Png)]
    [DataRow(SKEncodedImageFormat.Webp)]
    public async Task ResizeImageAsync_OpaqueLargeImage_IsScaledToFitAndSavedAsJpeg(SKEncodedImageFormat format)
    {
        var input = EncodeSolid(1000, 500, SKColors.CornflowerBlue, format);

        using var output = await _service.ResizeImageAsync(new MemoryStream(input), 350, 350);

        Assert.AreEqual("image/jpeg", output.ContentType);
        Assert.AreEqual(".jpg", output.FileExtension);
        using var codec = SKCodec.Create(output.Content);
        Assert.IsNotNull(codec);
        Assert.AreEqual(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
        Assert.AreEqual(350, codec.Info.Width);
        Assert.AreEqual(175, codec.Info.Height);
    }

    [TestMethod]
    public async Task ResizeImageAsync_TallImage_IsScaledToFitMaxHeight()
    {
        var input = EncodeSolid(300, 700, SKColors.CornflowerBlue, SKEncodedImageFormat.Png);

        using var output = await _service.ResizeImageAsync(new MemoryStream(input), 350, 350);

        using var bitmap = SKBitmap.Decode(output.Content);
        Assert.AreEqual(150, bitmap.Width);
        Assert.AreEqual(350, bitmap.Height);
    }

    [TestMethod]
    [DataRow(SKEncodedImageFormat.Png)]
    [DataRow(SKEncodedImageFormat.Webp)]
    public async Task ResizeImageAsync_TransparentLargeImage_IsSavedAsPngKeepingTransparency(SKEncodedImageFormat format)
    {
        // Left half fully transparent, right half opaque red
        using var source = new SKBitmap(1000, 500);
        source.Erase(SKColors.Transparent);
        using (var canvas = new SKCanvas(source))
        using (var paint = new SKPaint { Color = SKColors.Red })
            canvas.DrawRect(500, 0, 500, 500, paint);
        using var encoded = source.Encode(format, 100);

        using var output = await _service.ResizeImageAsync(new MemoryStream(encoded.ToArray()), 350, 350);

        Assert.AreEqual("image/png", output.ContentType);
        Assert.AreEqual(".png", output.FileExtension);
        using var codec = SKCodec.Create(output.Content);
        Assert.AreEqual(SKEncodedImageFormat.Png, codec.EncodedFormat);
        using var bitmap = SKBitmap.Decode(codec);
        Assert.AreEqual(350, bitmap.Width);
        Assert.AreEqual(175, bitmap.Height);
        Assert.AreEqual(0, bitmap.GetPixel(80, 87).Alpha, "transparent half should stay transparent");
        AssertColorNear(SKColors.Red, bitmap.GetPixel(270, 87), "opaque half");
        Assert.AreEqual(255, bitmap.GetPixel(270, 87).Alpha);
    }

    [TestMethod]
    public async Task ResizeImageAsync_PngWithUnusedAlphaChannel_IsSavedAsJpeg()
    {
        var input = EncodeSolid(1000, 500, SKColors.CornflowerBlue, SKEncodedImageFormat.Png);
        using (var inputCodec = SKCodec.Create(new MemoryStream(input)))
            Assert.AreNotEqual(SKAlphaType.Opaque, inputCodec.Info.AlphaType, "test PNG should have an alpha channel");

        using var output = await _service.ResizeImageAsync(new MemoryStream(input), 350, 350);

        Assert.AreEqual("image/jpeg", output.ContentType);
        Assert.AreEqual(".jpg", output.FileExtension);
    }

    [TestMethod]
    [DataRow("png", 100, 50)]
    [DataRow("jpeg", 100, 50)]
    [DataRow("webp", 100, 50)]
    [DataRow("bmp", 3, 2)]
    public async Task ResizeImageAsync_OpaqueImageWithinBounds_IsReencodedAsJpegAtSameSize(string format, int width, int height)
    {
        var input = format switch
        {
            "png" => EncodeSolid(width, height, SKColors.Orange, SKEncodedImageFormat.Png),
            "jpeg" => EncodeSolid(width, height, SKColors.Orange, SKEncodedImageFormat.Jpeg),
            "webp" => EncodeSolid(width, height, SKColors.Orange, SKEncodedImageFormat.Webp),
            "bmp" => CreateBmp(width, height),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        using var output = await _service.ResizeImageAsync(new MemoryStream(input), 350, 350);

        Assert.AreEqual(0, output.Content.Position);
        Assert.AreEqual("image/jpeg", output.ContentType);
        Assert.AreEqual(".jpg", output.FileExtension);
        using var codec = SKCodec.Create(new MemoryStream(output.Content.ToArray()));
        Assert.AreEqual(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
        Assert.AreEqual(width, codec.Info.Width);
        Assert.AreEqual(height, codec.Info.Height);
    }

    [TestMethod]
    public async Task ResizeImageAsync_GifWithinBounds_IsReencoded()
    {
        using var output = await _service.ResizeImageAsync(new MemoryStream(TinyGif), 350, 350);

        CollectionAssert.AreNotEqual(TinyGif, output.Content.ToArray());
        using var codec = SKCodec.Create(new MemoryStream(output.Content.ToArray()));
        Assert.AreNotEqual(SKEncodedImageFormat.Gif, codec.EncodedFormat);
        Assert.AreEqual(1, codec.Info.Width);
        Assert.AreEqual(1, codec.Info.Height);
    }

    [TestMethod]
    public async Task ResizeImageAsync_JpegWithinBoundsWithExifMetadata_DropsTheMetadata()
    {
        var input = WithExifOrientation(EncodeSolid(100, 50, SKColors.Orange, SKEncodedImageFormat.Jpeg), orientation: 1);
        Assert.IsTrue(ContainsExif(input), "test JPEG should have EXIF metadata");

        using var output = await _service.ResizeImageAsync(new MemoryStream(input), 350, 350);

        Assert.IsFalse(ContainsExif(output.Content.ToArray()));
    }

    [TestMethod]
    public async Task ResizeImageAsync_MorePixelsThanAllowed_ThrowsBeforeDecoding()
    {
        // 10,000 × 10,000 is 100 megapixels; only the header is needed to reject it
        var input = CreateBmpHeader(10_000, 10_000);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _service.ResizeImageAsync(new MemoryStream(input), 350, 350));

        Assert.IsInstanceOfType<InvalidDataException>(ex.InnerException);
        StringAssert.Contains(ex.InnerException.Message, "more than");
    }

    [TestMethod]
    public async Task ResizeImageAsync_ImageWithinBoundsInNonWebFormat_IsReencoded()
    {
        var input = CreateIco(EncodeSolid(16, 16, SKColors.Orange, SKEncodedImageFormat.Png));

        using var output = await _service.ResizeImageAsync(new MemoryStream(input), 350, 350);

        Assert.AreEqual("image/jpeg", output.ContentType);
        Assert.AreEqual(".jpg", output.FileExtension);
        using var codec = SKCodec.Create(output.Content);
        Assert.AreEqual(SKEncodedImageFormat.Jpeg, codec.EncodedFormat);
        Assert.AreEqual(16, codec.Info.Width);
    }

    [TestMethod]
    public async Task ResizeImageAsync_NonImageData_ThrowsInvalidOperationException()
    {
        var input = "definitely not an image"u8.ToArray();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => _service.ResizeImageAsync(new MemoryStream(input), 350, 350));
    }

    [TestMethod]
    public void GetSafeImageFileName_UsesGivenExtension()
    {
        var fileName = ImageService.GetSafeImageFileName(7, ".png");

        StringAssert.Matches(fileName, new System.Text.RegularExpressions.Regex(@"^person_7_\d{14}\.png$"));
    }

    /// <summary>
    /// Source is a 400x200 JPEG with quadrants R(ed) B(lue) / G(reen) Y(ellow). Expected corners of the
    /// upright output (top-left, top-right, bottom-left, bottom-right) follow the EXIF 2.3 orientation definitions.
    /// </summary>
    [TestMethod]
    [DataRow(1, "RBGY", false)]
    [DataRow(2, "BRYG", false)]
    [DataRow(3, "YGBR", false)]
    [DataRow(4, "GYRB", false)]
    [DataRow(5, "RGBY", true)]
    [DataRow(6, "GRYB", true)]
    [DataRow(7, "YBGR", true)]
    [DataRow(8, "BYRG", true)]
    public async Task ResizeImageAsync_ExifOrientation_IsAppliedToPixels(int orientation, string expectedCorners, bool portrait)
    {
        var input = WithExifOrientation(EncodeQuadrants(400, 200), orientation);

        using var output = await _service.ResizeImageAsync(new MemoryStream(input), 100, 100);

        using var bitmap = SKBitmap.Decode(output.Content);
        Assert.AreEqual(portrait ? 50 : 100, bitmap.Width);
        Assert.AreEqual(portrait ? 100 : 50, bitmap.Height);

        int qx = bitmap.Width / 4, qy = bitmap.Height / 4;
        AssertColorNear(ColorFor(expectedCorners[0]), bitmap.GetPixel(qx, qy), "top-left");
        AssertColorNear(ColorFor(expectedCorners[1]), bitmap.GetPixel(3 * qx, qy), "top-right");
        AssertColorNear(ColorFor(expectedCorners[2]), bitmap.GetPixel(qx, 3 * qy), "bottom-left");
        AssertColorNear(ColorFor(expectedCorners[3]), bitmap.GetPixel(3 * qx, 3 * qy), "bottom-right");
    }

    private static byte[] EncodeSolid(int width, int height, SKColor color, SKEncodedImageFormat format)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(color);
        using var data = bitmap.Encode(format, 100);
        return data.ToArray();
    }

    private static byte[] EncodeQuadrants(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using (var canvas = new SKCanvas(bitmap))
        {
            int hw = width / 2, hh = height / 2;
            using var paint = new SKPaint();
            paint.Color = SKColors.Red; canvas.DrawRect(0, 0, hw, hh, paint);
            paint.Color = SKColors.Blue; canvas.DrawRect(hw, 0, hw, hh, paint);
            paint.Color = SKColors.Lime; canvas.DrawRect(0, hh, hw, hh, paint);
            paint.Color = SKColors.Yellow; canvas.DrawRect(hw, hh, hw, hh, paint);
        }
        using var data = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100);
        return data.ToArray();
    }

    /// <summary>
    /// Inserts a minimal EXIF APP1 segment containing only the Orientation tag right after the JPEG SOI marker.
    /// </summary>
    private static byte[] WithExifOrientation(byte[] jpeg, int orientation)
    {
        byte[] app1 =
        [
            0xFF, 0xE1, 0x00, 0x22,                         // APP1, length 34
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0,
            0x49, 0x49, 0x2A, 0x00, 0x08, 0x00, 0x00, 0x00, // TIFF header (little endian), IFD0 at offset 8
            0x01, 0x00,                                     // 1 IFD entry
            0x12, 0x01, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, // Orientation (0x0112), SHORT, count 1
            (byte)orientation, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00                          // no next IFD
        ];
        return [.. jpeg[..2], .. app1, .. jpeg[2..]];
    }

    /// <summary>
    /// True if the JPEG has an EXIF (APP1 "Exif") segment
    /// </summary>
    private static bool ContainsExif(byte[] jpeg)
    {
        byte[] marker = [0xFF, 0xE1, 0, 0, (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0];
        for (int i = 0; i + marker.Length <= jpeg.Length; i++)
        {
            if (jpeg[i] == 0xFF && jpeg[i + 1] == 0xE1 && jpeg.AsSpan(i + 4, 6).SequenceEqual(marker.AsSpan(4)))
                return true;
        }
        return false;
    }

    /// <summary>
    /// Builds just the header of a 24-bit BMP, which is enough for Skia to read its size
    /// </summary>
    private static byte[] CreateBmpHeader(int width, int height)
    {
        int pixelBytes = ((width * 3 + 3) & ~3) * height;
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)'B'); writer.Write((byte)'M');
        writer.Write(54 + pixelBytes); writer.Write(0); writer.Write(54);
        writer.Write(40); writer.Write(width); writer.Write(height);
        writer.Write((short)1); writer.Write((short)24); writer.Write(0); writer.Write(pixelBytes);
        writer.Write(2835); writer.Write(2835); writer.Write(0); writer.Write(0);
        writer.Flush();
        return ms.ToArray();
    }

    /// <summary>
    /// Builds an uncompressed 24-bit BMP; Skia can't encode BMPs.
    /// </summary>
    private static byte[] CreateBmp(int width, int height)
    {
        int rowSize = (width * 3 + 3) & ~3;
        int pixelBytes = rowSize * height;
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((byte)'B'); writer.Write((byte)'M');
        writer.Write(54 + pixelBytes); writer.Write(0); writer.Write(54);
        writer.Write(40); writer.Write(width); writer.Write(height);
        writer.Write((short)1); writer.Write((short)24); writer.Write(0); writer.Write(pixelBytes);
        writer.Write(2835); writer.Write(2835); writer.Write(0); writer.Write(0);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++) { writer.Write((byte)0x20); writer.Write((byte)0x80); writer.Write((byte)0xE0); }
            for (int p = width * 3; p < rowSize; p++) writer.Write((byte)0);
        }
        writer.Flush();
        return ms.ToArray();
    }

    /// <summary>
    /// Wraps a PNG in a single-image ICO file; Skia can't encode ICOs.
    /// </summary>
    private static byte[] CreateIco(byte[] png)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((short)0); writer.Write((short)1); writer.Write((short)1); // reserved, type icon, 1 image
        writer.Write((byte)16); writer.Write((byte)16); writer.Write((byte)0); writer.Write((byte)0);
        writer.Write((short)1); writer.Write((short)32); writer.Write(png.Length); writer.Write(22);
        writer.Write(png);
        writer.Flush();
        return ms.ToArray();
    }

    private static SKColor ColorFor(char c) => c switch
    {
        'R' => SKColors.Red,
        'B' => SKColors.Blue,
        'G' => SKColors.Lime,
        'Y' => SKColors.Yellow,
        _ => throw new ArgumentOutOfRangeException(nameof(c))
    };

    private static void AssertColorNear(SKColor expected, SKColor actual, string where, int tolerance = 40)
    {
        bool near = Math.Abs(expected.Red - actual.Red) <= tolerance
            && Math.Abs(expected.Green - actual.Green) <= tolerance
            && Math.Abs(expected.Blue - actual.Blue) <= tolerance;
        Assert.IsTrue(near, $"{where}: expected {expected} but was {actual}");
    }
}
