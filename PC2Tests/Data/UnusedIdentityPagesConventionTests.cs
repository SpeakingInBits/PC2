using Microsoft.AspNetCore.Mvc.ApplicationModels;
using PC2.Data;

namespace PC2Tests.Data;

[TestClass]
public class UnusedIdentityPagesConventionTests
{
    private static PageRouteModel CreatePage(string viewEnginePath, string? areaName)
    {
        var model = new PageRouteModel($"/Areas/{areaName}/Pages{viewEnginePath}.cshtml", viewEnginePath, areaName);
        model.Selectors.Add(new SelectorModel { AttributeRouteModel = new AttributeRouteModel { Template = viewEnginePath.TrimStart('/') } });
        return model;
    }

    [TestMethod]
    [DataRow("/Account/Register")]
    [DataRow("/Account/RegisterConfirmation")]
    [DataRow("/Account/ConfirmEmail")]
    [DataRow("/Account/ForgotPassword")]
    [DataRow("/Account/ResetPassword")]
    [DataRow("/Account/ResendEmailConfirmation")]
    [DataRow("/Account/ExternalLogin")]
    public void Apply_UnusedIdentityPage_RemovesRoutes(string viewEnginePath)
    {
        PageRouteModel page = CreatePage(viewEnginePath, "Identity");

        new UnusedIdentityPagesConvention().Apply(page);

        Assert.AreEqual(0, page.Selectors.Count);
    }

    [TestMethod]
    [DataRow("/Account/Login")]
    [DataRow("/Account/Logout")]
    [DataRow("/Account/Lockout")]
    [DataRow("/Account/AccessDenied")]
    [DataRow("/Account/LoginWith2fa")]
    [DataRow("/Account/Manage/Index")]
    [DataRow("/Account/Manage/EnableAuthenticator")]
    public void Apply_IdentityPageInUse_KeepsRoutes(string viewEnginePath)
    {
        PageRouteModel page = CreatePage(viewEnginePath, "Identity");

        new UnusedIdentityPagesConvention().Apply(page);

        Assert.AreEqual(1, page.Selectors.Count);
    }

    [TestMethod]
    public void Apply_SamePathOutsideIdentityArea_KeepsRoutes()
    {
        PageRouteModel page = CreatePage("/Account/Register", null);

        new UnusedIdentityPagesConvention().Apply(page);

        Assert.AreEqual(1, page.Selectors.Count);
    }
}
