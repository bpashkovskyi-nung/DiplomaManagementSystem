using DiplomaManagementSystem.Application.Options;

using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Web.Departments;

public interface IDepartmentSessionService
{
    Guid? GetSelectedDepartmentId(HttpContext httpContext);

    void SetSelectedDepartment(HttpContext httpContext, Guid departmentId, bool superAdminImpersonating = false);

    void ClearSelectedDepartment(HttpContext httpContext);
}

internal sealed class DepartmentSessionService(IOptions<DepartmentOptions> options) : IDepartmentSessionService
{
    public Guid? GetSelectedDepartmentId(HttpContext httpContext)
    {
        string cookieName = options.Value.SelectedDepartmentCookieName;
        if (!httpContext.Request.Cookies.TryGetValue(cookieName, out string? value)
            || !Guid.TryParse(value, out Guid departmentId))
        {
            return null;
        }

        return departmentId;
    }

    public void SetSelectedDepartment(HttpContext httpContext, Guid departmentId, bool superAdminImpersonating = false)
    {
        DepartmentOptions departmentOptions = options.Value;
        CookieOptions cookieOptions = new()
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = httpContext.Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddDays(departmentOptions.DepartmentCookieExpirationDays),
        };

        httpContext.Response.Cookies.Append(
            departmentOptions.SelectedDepartmentCookieName,
            departmentId.ToString(),
            cookieOptions);
        httpContext.Items[DepartmentContextKeys.SelectedDepartmentId] = departmentId;

        if (superAdminImpersonating)
        {
            httpContext.Items[DepartmentContextKeys.SuperAdminImpersonating] = true;
            httpContext.Response.Cookies.Append(
                departmentOptions.ImpersonationCookieName,
                "1",
                cookieOptions);
        }
        else
        {
            httpContext.Items.Remove(DepartmentContextKeys.SuperAdminImpersonating);
            httpContext.Response.Cookies.Delete(departmentOptions.ImpersonationCookieName);
        }
    }

    public void ClearSelectedDepartment(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(options.Value.SelectedDepartmentCookieName);
        httpContext.Response.Cookies.Delete(options.Value.ImpersonationCookieName);
        httpContext.Items.Remove(DepartmentContextKeys.SelectedDepartmentId);
        httpContext.Items.Remove(DepartmentContextKeys.SuperAdminImpersonating);
    }
}
