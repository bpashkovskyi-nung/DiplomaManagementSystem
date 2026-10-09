using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;
using DiplomaManagementSystem.Web.Secretary;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Areas.Secretary.Controllers;

public sealed class SessionController(
    ISecretarySessionService sessionService,
    ISecretaryAccessService accessService) : SecretaryControllerBase(sessionService, accessService)
{
    [HttpGet]
    public async Task<IActionResult> Select(CancellationToken cancellationToken)
    {
        Guid userId = GetUserId();
        IReadOnlyList<SecretarySessionOptionDto> sessions =
            await AccessService.GetAccessibleSessionsAsync(userId, cancellationToken);

        if (sessions.Count == 0)
        {
            return View(new SessionSelectViewModel());
        }

        Guid? selectedSessionId = SessionService.GetSelectedSessionId(HttpContext);
        if (selectedSessionId is not null
            && sessions.Any(session => session.Id == selectedSessionId.Value))
        {
            return RedirectToAction("Index", "Dashboard");
        }

        if (sessions.Count == 1)
        {
            await SessionService.SetSelectedSessionAsync(
                HttpContext,
                userId,
                sessions[0].Id,
                cancellationToken);
            return RedirectToAction("Index", "Dashboard");
        }

        SessionSelectViewModel model = new()
        {
            Sessions = sessions
                .Select(session => new SelectListItem(session.Label, session.Id.ToString()))
                .ToList(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(Guid sessionId, CancellationToken cancellationToken)
    {
        Guid userId = GetUserId();
        await SessionService.SetSelectedSessionAsync(HttpContext, userId, sessionId, cancellationToken);
        return RedirectToAction("Index", "Dashboard");
    }
}
