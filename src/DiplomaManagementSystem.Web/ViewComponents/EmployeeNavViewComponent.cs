using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Web.AdminPreview;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.ViewComponents;

public sealed class EmployeeNavViewComponent(IAdminPreviewService adminPreviewService) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        if (User.Identity?.IsAuthenticated != true || !User.IsInRole(RoleNames.Employee))
        {
            return Content(string.Empty);
        }

        if (!AdminPreviewModeRules.CanShowAreaNav(adminPreviewService, HttpContext, UserClaimsPrincipal, "Employee"))
        {
            return Content(string.Empty);
        }

        return View();
    }
}
