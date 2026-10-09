using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Web.AdminPreview;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.ViewComponents;

public sealed class SuperAdminNavViewComponent(IAdminPreviewService adminPreviewService) : ViewComponent
{
    public IViewComponentResult Invoke()
    {
        if (User.Identity?.IsAuthenticated != true
            || !UserClaimsPrincipal.IsInRole(RoleNames.SuperAdmin))
        {
            return Content(string.Empty);
        }

        if (adminPreviewService.GetMode(HttpContext) != AdminPreviewMode.SuperAdmin)
        {
            return Content(string.Empty);
        }

        return View();
    }
}
