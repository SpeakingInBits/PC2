using Microsoft.AspNetCore.Http;
using PC2.Models;
using System.Text;

namespace PC2Tests.Models;

[TestClass]
public class PdfFileValidatorTests
{
    private const string PdfContents = "%PDF-1.7\n%test file";

    [TestMethod]
    [DataRow("newsletter.pdf")]
    [DataRow("NEWSLETTER.PDF")]
    public void IsPdf_PdfFile_ReturnsTrue(string fileName)
    {
        IFormFile file = CreateFile(fileName, PdfContents);

        Assert.IsTrue(PdfFileValidator.IsPdf(file));
    }

    [TestMethod]
    [DataRow("newsletter.docx")]
    [DataRow("newsletter")]
    [DataRow("newsletter.pdf.exe")]
    public void IsPdf_WrongExtension_ReturnsFalse(string fileName)
    {
        IFormFile file = CreateFile(fileName, PdfContents);

        Assert.IsFalse(PdfFileValidator.IsPdf(file));
    }

    [TestMethod]
    public void IsPdf_RenamedNonPdfFile_ReturnsFalse()
    {
        IFormFile file = CreateFile("newsletter.pdf", "PK\u0003\u0004 a zip or Word file");

        Assert.IsFalse(PdfFileValidator.IsPdf(file));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("%PD")]
    public void IsPdf_EmptyOrTooShortFile_ReturnsFalse(string contents)
    {
        IFormFile file = CreateFile("newsletter.pdf", contents);

        Assert.IsFalse(PdfFileValidator.IsPdf(file));
    }

    [TestMethod]
    public void IsPdf_CanStillReadFileAfterward()
    {
        IFormFile file = CreateFile("newsletter.pdf", PdfContents);

        PdfFileValidator.IsPdf(file);

        // The upload opens the file again after validation, so it must start from the beginning
        using StreamReader reader = new(file.OpenReadStream());
        Assert.AreEqual(PdfContents, reader.ReadToEnd());
    }

    private static IFormFile CreateFile(string fileName, string contents)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(contents);
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
    }
}
