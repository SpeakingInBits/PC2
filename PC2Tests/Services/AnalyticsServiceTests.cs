using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PC2.Services;

namespace PC2Tests.Services;

[TestClass]
public class AnalyticsServiceTests
{
    [TestMethod]
    public void GetCacheKey_DefaultRangeRequestedMinutesApart_GivesSameKey()
    {
        // The default view is the 30 days up to the current second
        var first = new DateTime(2026, 10, 18, 10, 1, 5);
        var second = new DateTime(2026, 10, 18, 10, 8, 59);

        Assert.AreEqual(
            AnalyticsService.GetCacheKey(first.AddDays(-30), first),
            AnalyticsService.GetCacheKey(second.AddDays(-30), second));
    }

    [TestMethod]
    public void GetCacheKey_RangeRequestedAfterCacheDuration_GivesNewKey()
    {
        var first = new DateTime(2026, 10, 18, 10, 1, 5);
        var later = first + AnalyticsService.CacheDuration;

        Assert.AreNotEqual(
            AnalyticsService.GetCacheKey(first.AddDays(-30), first),
            AnalyticsService.GetCacheKey(later.AddDays(-30), later));
    }

    [TestMethod]
    public void GetCacheKey_DifferentMonths_GiveDifferentKeys()
    {
        var october = new DateTime(2026, 10, 1);
        var november = new DateTime(2026, 11, 1);

        Assert.AreNotEqual(
            AnalyticsService.GetCacheKey(october, november.AddSeconds(-1)),
            AnalyticsService.GetCacheKey(november, november.AddMonths(1).AddSeconds(-1)));
    }

    [TestMethod]
    public async Task GetAnalyticsDataAsync_WithoutAzure_ReturnsSampleDataForTheRequestedRange()
    {
        var service = new AnalyticsService(new ConfigurationBuilder().Build(),
            new MemoryCache(new MemoryCacheOptions()), NullLogger<AnalyticsService>.Instance);
        var start = new DateTime(2026, 9, 1);
        var end = new DateTime(2026, 9, 30, 23, 59, 59);

        var result = await service.GetAnalyticsDataAsync(start, end);

        Assert.AreEqual(start, result.StartDate);
        Assert.AreEqual(end, result.EndDate);
        Assert.IsTrue(result.TotalPageViews > 0);
    }
}
