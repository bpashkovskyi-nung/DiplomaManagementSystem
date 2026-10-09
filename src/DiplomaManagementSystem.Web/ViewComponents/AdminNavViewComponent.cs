using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Web.AdminPreview;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.ViewComponents;

public sealed class AdminNavViewComponent(
    IAdminPreviewService adminPreviewService,
    IDepartmentContext departmentContext) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        if (adminPreviewService.GetMode(HttpContext) != AdminPreviewMode.Admin)
        {
            return Content(string.Empty);
        }

        bool canShow = UserClaimsPrincipal.IsInRole(RoleNames.Admin)
                       || (UserClaimsPrincipal.IsInRole(RoleNames.SuperAdmin)
                           && departmentContext.IsSuperAdminImpersonating);

        if (!canShow)
        {
            return Content(string.Empty);
        }

        return View();
    }
}
