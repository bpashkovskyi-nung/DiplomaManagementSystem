using DiplomaManagementSystem.Web.AdminPreview;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DiplomaManagementSystem.Web.Filters;

internal sealed class AdminPreviewImpersonationFilter(IAdminPreviewService adminPreviewService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        HttpContext httpContext = context.HttpContext;
        if (!adminPreviewService.IsAdmin(httpContext.User, httpContext))
        {
            await next();
            return;
        }

        AdminPreviewMode mode = adminPreviewService.GetMode(httpContext);
        if (!adminPreviewService.IsActivePreview(httpContext))
        {
            await next();
            return;
        }

        string? area = context.RouteData.Values["area"]?.ToString();
        if (area is "Admin" or "Student" or "Employee" or "Secretary" or "SuperAdmin"
            && !AdminPreviewModeRules.AreaMatchesMode(area, mode))
        {
            context.Result = CreateModeHomeRedirect(mode);
            return;
        }

        if (!adminPreviewService.RequiresImpersonation(mode))
        {
            await next();
            return;
        }

        if (!AdminPreviewModeRules.AreaMatchesMode(area ?? string.Empty, mode))
        {
            await next();
            return;
        }

        if (adminPreviewService.HasImpersonation(httpContext))
        {
            await next();
            return;
        }

        string returnUrl = AdminPreviewReturnUrl.Build(httpContext);
        context.Result = new RedirectToActionResult(
            "SelectUser",
            "AdminPreview",
            new { mode, returnUrl });
    }

    private static RedirectToActionResult CreateModeHomeRedirect(AdminPreviewMode mode) => AdminPreviewModeRules.Normalize(mode) switch
    {
        AdminPreviewMode.SuperAdmin => new RedirectToActionResult("Index", "Home", new { area = "SuperAdmin" }),
        AdminPreviewMode.Student => new RedirectToActionResult("Index", "Diploma", new { area = "Student" }),
        AdminPreviewMode.Secretary => new RedirectToActionResult("Index", "Dashboard", new { area = "Secretary" }),
        AdminPreviewMode.Employee => new RedirectToActionResult("Index", "Home", new { area = "Employee" }),
        _ => new RedirectToActionResult("Index", "Home", new { area = "Admin" }),
    };
}
