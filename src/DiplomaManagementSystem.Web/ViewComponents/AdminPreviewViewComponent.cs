using DiplomaManagementSystem.Application.AdminPreview.Contracts;
using DiplomaManagementSystem.Web.AdminPreview;
using DiplomaManagementSystem.Web.Models;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.ViewComponents;

public sealed class AdminPreviewViewComponent(
    IAdminPreviewService adminPreviewService,
    IAdminPreviewUserLookup adminPreviewUserLookup) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string part = "switcher")
    {
        if (!adminPreviewService.IsAdmin(UserClaimsPrincipal))
        {
            return Content(string.Empty);
        }

        AdminPreviewMode currentMode = adminPreviewService.GetMode(HttpContext);
        AdminPreviewMode[] modes =
        [
            AdminPreviewMode.SuperAdmin,
            AdminPreviewMode.Admin,
            AdminPreviewMode.Secretary,
            AdminPreviewMode.Employee,
        ];

        string? impersonatedDisplay = null;
        string? impersonatedEmail = null;
        if (adminPreviewService.GetImpersonatedUserId(HttpContext) is Guid impersonatedUserId)
        {
            AdminPreviewUserProfile? impersonatedUser = await adminPreviewUserLookup.FindAsync(impersonatedUserId);
            impersonatedDisplay = impersonatedUser?.FullName;
            impersonatedEmail = impersonatedUser?.Email;
        }

        AdminPreviewViewModel model = new()
        {
            CurrentMode = currentMode,
            IsActivePreview = adminPreviewService.IsActivePreview(HttpContext),
            CurrentModeDisplay = adminPreviewService.GetModeDisplayName(currentMode),
            ImpersonatedUserDisplay = impersonatedDisplay,
            ImpersonatedUserEmail = impersonatedEmail,
            RequiresUserSelection = adminPreviewService.RequiresImpersonation(currentMode),
            SelectUserMode = adminPreviewService.RequiresImpersonation(currentMode) ? currentMode : null,
            ReturnUrl = AdminPreviewReturnUrl.Build(HttpContext),
            Options = modes
                .Select(mode => new AdminPreviewOptionViewModel
                {
                    Mode = mode,
                    DisplayName = adminPreviewService.GetModeDisplayName(mode),
                    IsSelected = mode == currentMode,
                })
                .ToList(),
        };

        return part == "banner" ? View("Banner", model) : View("Switcher", model);
    }
}
