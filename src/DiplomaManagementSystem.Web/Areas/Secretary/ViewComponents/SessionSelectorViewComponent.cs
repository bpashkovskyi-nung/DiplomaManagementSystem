using System.Security.Claims;

using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;
using DiplomaManagementSystem.Web.Secretary;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Areas.Secretary.ViewComponents;

public sealed class SessionSelectorViewComponent(
    ISecretarySessionService sessionService,
    ISecretaryAccessService accessService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        string? userIdValue = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            return Content(string.Empty);
        }

        IReadOnlyList<SecretarySessionOptionDto> sessions =
            await accessService.GetAccessibleSessionsAsync(userId, HttpContext.RequestAborted);

        if (sessions.Count == 0)
        {
            return Content(string.Empty);
        }

        Guid? selectedSessionId = sessionService.GetSelectedSessionId(HttpContext);
        SessionSelectorViewModel model = new()
        {
            SelectedSessionId = selectedSessionId,
            Sessions = sessions
                .Select(session => new SelectListItem(
                    session.Label,
                    session.Id.ToString(),
                    session.Id == selectedSessionId))
                .ToList(),
        };

        return View(model);
    }
}
