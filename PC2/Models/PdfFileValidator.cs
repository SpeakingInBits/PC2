namespace PC2.Models;

/// <summary>
/// Checks that an uploaded file is a PDF before it is saved.
/// </summary>
public static class PdfFileValidator
{
    /// <summary>
    /// Every PDF file starts with these bytes
    /// </summary>
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();

    /// <summary>
    /// Returns true when the file has a .pdf extension and its contents start like a PDF.
    /// Checking the contents stops other files from being uploaded just by renaming them to .pdf
    /// </summary>
    /// <param name="file">The uploaded file.</param>
    public static bool IsPdf(IFormFile file)
    {
        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        using Stream stream = file.OpenReadStream();
        byte[] header = new byte[PdfSignature.Length];
        int bytesRead = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);
        return bytesRead == header.Length && header.SequenceEqual(PdfSignature);
    }
}
