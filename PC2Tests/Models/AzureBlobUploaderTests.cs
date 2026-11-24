using PC2.Models;
using System.Text.RegularExpressions;

namespace PC2Tests.Models;

[TestClass]
public class AzureBlobUploaderTests
{
    [TestMethod]
    [DataRow("Fall Newsletter.pdf", "Fall-Newsletter")]
    [DataRow("Focus_2026-10.pdf", "Focus_2026-10")]
    [DataRow("Résumé (final).PDF", "R-sum-final")]
    public void CreateUniqueBlobName_KeepsReadableNameAndAddsRandomPart(string fileName, string expectedReadableName)
    {
        string blobName = AzureBlobUploader.CreateUniqueBlobName(fileName, ".pdf");

        StringAssert.Matches(blobName, new Regex($"^{Regex.Escape(expectedReadableName)}-[0-9a-f]{{12}}\\.pdf$"));
    }

    [TestMethod]
    [DataRow("../other/x.pdf", "x")]
    [DataRow(@"..\..\x.pdf", "x")]
    [DataRow(@"C:\Users\Staff\Documents\Flyer.pdf", "Flyer")]
    [DataRow("..%2Fother%2Fx.pdf", "2Fother-2Fx")]
    public void CreateUniqueBlobName_IgnoresFolderPaths(string fileName, string expectedReadableName)
    {
        string blobName = AzureBlobUploader.CreateUniqueBlobName(fileName, ".pdf");

        StringAssert.StartsWith(blobName, expectedReadableName + "-");
        Assert.IsTrue(AzureBlobUploader.IsSafeBlobName(blobName));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(".pdf")]
    [DataRow("....pdf")]
    [DataRow("../")]
    public void CreateUniqueBlobName_NoUsableName_UsesFile(string? fileName)
    {
        string blobName = AzureBlobUploader.CreateUniqueBlobName(fileName, ".pdf");

        StringAssert.Matches(blobName, new Regex("^file-[0-9a-f]{12}\\.pdf$"));
    }

    [TestMethod]
    public void CreateUniqueBlobName_LongName_IsShortened()
    {
        string blobName = AzureBlobUploader.CreateUniqueBlobName(new string('a', 300) + ".pdf", ".pdf");

        Assert.AreEqual(80 + "-".Length + 12 + ".pdf".Length, blobName.Length);
    }

    [TestMethod]
    public void CreateUniqueBlobName_SameFileName_GivesDifferentNames()
    {
        string first = AzureBlobUploader.CreateUniqueBlobName("Flyer.pdf", ".pdf");
        string second = AzureBlobUploader.CreateUniqueBlobName("Flyer.pdf", ".pdf");

        Assert.AreNotEqual(first, second);
    }

    [TestMethod]
    [DataRow("Flyer.pdf")]
    [DataRow("My File (2).pdf")]
    [DataRow("person_12_20261018093000.jpg")]
    [DataRow("report..final.pdf")]
    public void IsSafeBlobName_NameInsideContainer_ReturnsTrue(string blobName)
    {
        Assert.IsTrue(AzureBlobUploader.IsSafeBlobName(blobName));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow(".")]
    [DataRow("..")]
    [DataRow("../other/x.pdf")]
    [DataRow("folder/x.pdf")]
    [DataRow(@"..\x.pdf")]
    public void IsSafeBlobName_NameOutsideContainerOrEmpty_ReturnsFalse(string? blobName)
    {
        Assert.IsFalse(AzureBlobUploader.IsSafeBlobName(blobName));
    }
}
