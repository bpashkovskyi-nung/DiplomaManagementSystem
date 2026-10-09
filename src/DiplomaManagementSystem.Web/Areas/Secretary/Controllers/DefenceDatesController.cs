using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Web.Secretary;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Secretary.Controllers;

public sealed class DefenceDatesController(
    ISecretarySessionService sessionService,
    ISecretaryAccessService accessService,
    IDefenceDatePreferenceQueueService preferenceQueueService)
    : SecretaryControllerBase(sessionService, accessService)
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        DefenceDatePreferenceQueueDto? queue =
            await preferenceQueueService.GetQueueAsync(sessionId, cancellationToken);

        return queue is null ? RedirectToAction("Select", "Session") : View(queue);
    }
}
