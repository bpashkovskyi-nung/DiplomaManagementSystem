using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Web.Departments;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Web.Tests.Departments;

public sealed class DepartmentSessionServiceTests
{
    [Fact]
    public void GetSelectedDepartmentId_ReadsCookieValue()
    {
        DefaultHttpContext httpContext = new();
        var departmentId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        httpContext.Request.Headers.Cookie = $"dms.dept={departmentId}";
        DepartmentSessionService service = CreateService();

        Assert.Equal(departmentId, service.GetSelectedDepartmentId(httpContext));
    }

    [Fact]
    public void SetSelectedDepartment_SetsImpersonationFlag()
    {
        DefaultHttpContext httpContext = new();
        DepartmentSessionService service = CreateService();

        service.SetSelectedDepartment(httpContext, Guid.NewGuid(), superAdminImpersonating: true);

        Assert.True(httpContext.Items.ContainsKey("DepartmentContext.SuperAdminImpersonating"));
    }

    [Fact]
    public void SetSelectedDepartment_WithoutImpersonation_ClearsFlag()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Items["DepartmentContext.SuperAdminImpersonating"] = true;
        DepartmentSessionService service = CreateService();

        service.SetSelectedDepartment(httpContext, Guid.NewGuid(), superAdminImpersonating: false);

        Assert.False(httpContext.Items.ContainsKey("DepartmentContext.SuperAdminImpersonating"));
    }

    [Fact]
    public void ClearSelectedDepartment_RemovesCookieAndFlag()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers.Cookie = "dms.dept=aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        httpContext.Items["DepartmentContext.SuperAdminImpersonating"] = true;
        DepartmentSessionService service = CreateService();

        service.ClearSelectedDepartment(httpContext);

        Assert.False(httpContext.Items.ContainsKey("DepartmentContext.SuperAdminImpersonating"));
        string setCookie = httpContext.Response.Headers.SetCookie.ToString();
        Assert.Contains("dms.dept=", setCookie, StringComparison.Ordinal);
    }

    private static DepartmentSessionService CreateService() =>
        new(Options.Create(new DepartmentOptions
        {
            SelectedDepartmentCookieName = "dms.dept",
            DepartmentCookieExpirationDays = 30,
        }));
}
