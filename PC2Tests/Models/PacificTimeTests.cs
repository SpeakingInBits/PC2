using PC2.Models;

namespace PC2Tests.Models;

[TestClass]
public class PacificTimeTests
{
    [TestMethod]
    [DataRow("2026-10-23T03:00:00Z", "2026-10-22", DisplayName = "8 PM PDT is still the same day, though UTC is the next day")]
    [DataRow("2026-10-22T15:00:00Z", "2026-10-22", DisplayName = "8 AM PDT")]
    [DataRow("2026-10-23T07:30:00Z", "2026-10-23", DisplayName = "12:30 AM PDT is the next day")]
    [DataRow("2026-12-02T07:30:00Z", "2026-12-01", DisplayName = "11:30 PM PST in winter (UTC-8)")]
    public void Today_IsTheDateInPacificTime(string utcNow, string expectedDate)
    {
        Assert.AreEqual(DateOnly.Parse(expectedDate), PacificTime.Today(new FixedTimeProvider(utcNow)));
    }
}
