using System.Security.Claims;

using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.AdminPreview;
using DiplomaManagementSystem.Web.Models.Shared;
using DiplomaManagementSystem.Web.Secretary;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.ViewComponents;

public sealed class SecretaryNavViewComponent(
    ISecretaryAccessService accessService,
    ISecretarySessionService sessionService,
    IDefenceSessionQueries defenceSessionQueries,
    IAdminPreviewService adminPreviewService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        if (!AdminPreviewModeRules.CanShowAreaNav(adminPreviewService, HttpContext, UserClaimsPrincipal, "Secretary"))
        {
            return Content(string.Empty);
        }

        string? userIdValue = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            return Content(string.Empty);
        }

        bool isSecretary = await accessService.IsSecretaryAsync(userId, HttpContext.RequestAborted);
        if (!isSecretary)
        {
            return Content(string.Empty);
        }

        string worksLabel = SecretaryPageTitles.Works;
        Guid? selectedSessionId = sessionService.GetSelectedSessionId(HttpContext);
        if (selectedSessionId is Guid sessionId)
        {
            DefenceSessionType? sessionType = await defenceSessionQueries.GetTypeAsync(
                sessionId,
                HttpContext.RequestAborted);
            if (sessionType is not null)
            {
                worksLabel = DefenceWorkLabel.PluralCapitalized(sessionType.Value);
            }
        }

        return View(new SecretaryNavViewModel { WorksLabel = worksLabel });
    }
}
