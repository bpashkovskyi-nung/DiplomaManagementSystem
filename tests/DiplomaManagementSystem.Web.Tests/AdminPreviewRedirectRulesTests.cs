using DiplomaManagementSystem.Web.AdminPreview;

namespace DiplomaManagementSystem.Web.Tests;

public sealed class AdminPreviewRedirectRulesTests
{
    [Theory]
    [InlineData("/Student/Diploma", AdminPreviewMode.Admin, false)]
    [InlineData("/Employee/Home", AdminPreviewMode.Employee, true)]
    [InlineData("/Secretary/Dashboard", AdminPreviewMode.Secretary, true)]
    [InlineData("/Secretary/Dashboard", AdminPreviewMode.Employee, false)]
    [InlineData("/SuperAdmin/Home", AdminPreviewMode.SuperAdmin, true)]
    [InlineData("/Admin/DefenceSessions", AdminPreviewMode.Admin, true)]
    [InlineData("/Student/Diploma", AdminPreviewMode.Student, true)]
    [InlineData("/", AdminPreviewMode.Admin, true)]
    [InlineData("/Account/AccessDenied", AdminPreviewMode.Admin, false)]
    public void IsReturnUrlValidForMode_ReturnsExpected(string returnUrl, AdminPreviewMode mode, bool expected)
    {
        bool actual = AdminPreviewRedirectRules.IsReturnUrlValidForMode(returnUrl, mode);

        Assert.Equal(expected, actual);
    }
}
