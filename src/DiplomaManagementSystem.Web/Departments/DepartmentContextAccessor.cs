using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Options;

using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Web.Departments;

internal sealed class DepartmentContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IOptions<DepartmentOptions> options) : IDepartmentContext
{
    public Guid? CurrentDepartmentId
    {
        get
        {
            HttpContext? httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return null;
            }

            if (httpContext.Items.TryGetValue(DepartmentContextKeys.SelectedDepartmentId, out object? item)
                && item is Guid selectedFromItems)
            {
                return selectedFromItems;
            }

            string cookieName = options.Value.SelectedDepartmentCookieName;
            if (!httpContext.Request.Cookies.TryGetValue(cookieName, out string? value)
                || !Guid.TryParse(value, out Guid departmentId))
            {
                return null;
            }

            return departmentId;
        }
    }

    public bool IsSuperAdminImpersonating
    {
        get
        {
            HttpContext? httpContext = httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return false;
            }

            if (httpContext.Items.ContainsKey(DepartmentContextKeys.SuperAdminImpersonating))
            {
                return true;
            }

            return httpContext.Request.Cookies.ContainsKey(options.Value.ImpersonationCookieName);
        }
    }
}

internal static class DepartmentContextKeys
{
    public const string SuperAdminImpersonating = "DepartmentContext.SuperAdminImpersonating";

    public const string SelectedDepartmentId = "DepartmentContext.SelectedDepartmentId";
}
