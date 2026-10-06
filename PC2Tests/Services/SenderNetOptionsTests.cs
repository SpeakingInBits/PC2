using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel.DataAnnotations;

namespace PC2.Services.Tests;

[TestClass]
public class SenderNetOptionsTests
{
    [TestMethod]
    [DataRow(null, null, false)]
    [DataRow("", "", false)]
    [DataRow("75bcedda809181", "", false)]
    [DataRow("", "bYEg7W", false)]
    [DataRow("75bcedda809181", "bYEg7W", true)]
    public void IsConfigured_RequiresBothIds(string? accountId, string? signupFormId, bool expected)
    {
        var options = new SenderNetOptions { AccountId = accountId, SignupFormId = signupFormId };

        Assert.AreEqual(expected, options.IsConfigured);
    }

    [TestMethod]
    [DataRow(null, null)]
    [DataRow("", "")]
    [DataRow("75bcedda809181", "bYEg7W")]
    public void Validation_AllowsBlankOrAlphanumericIds(string? accountId, string? signupFormId)
    {
        var options = new SenderNetOptions { AccountId = accountId, SignupFormId = signupFormId };

        Assert.IsTrue(IsValid(options));
    }

    [TestMethod]
    [DataRow("75bc'); alert('x", "bYEg7W")]
    [DataRow("75bcedda809181", "bYEg7W\" onclick=\"x")]
    [DataRow("75bcedda 809181", "bYEg7W")]
    public void Validation_RejectsIdsThatAreNotAlphanumeric(string accountId, string signupFormId)
    {
        var options = new SenderNetOptions { AccountId = accountId, SignupFormId = signupFormId };

        Assert.IsFalse(IsValid(options));
    }

    private static bool IsValid(SenderNetOptions options) =>
        Validator.TryValidateObject(options, new ValidationContext(options), null, validateAllProperties: true);
}
