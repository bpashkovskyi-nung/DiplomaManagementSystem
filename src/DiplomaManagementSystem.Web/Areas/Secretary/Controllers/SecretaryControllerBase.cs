using System.Security.Claims;

using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Web.Secretary;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Secretary.Controllers;

[Area("Secretary")]
[Authorize(Policy = PolicyNames.ExamCommissionSecretary)]
public abstract class SecretaryControllerBase(
    ISecretarySessionService sessionService,
    ISecretaryAccessService accessService) : Controller
{
    protected ISecretarySessionService SessionService { get; } = sessionService;

    protected ISecretaryAccessService AccessService { get; } = accessService;

    protected Guid GetUserId()
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing.");
        }

        return userId;
    }

    protected async Task<(Guid SessionId, IActionResult? Redirect)> ResolveSessionAsync(
        CancellationToken cancellationToken)
    {
        Guid? sessionId = SessionService.GetSelectedSessionId(HttpContext);
        if (sessionId is null)
        {
            return (Guid.Empty, RedirectToAction("Select", "Session"));
        }

        Guid userId = GetUserId();
        if (!await AccessService.CanAccessSessionAsync(userId, sessionId.Value, cancellationToken))
        {
            return (Guid.Empty, RedirectToAction("Select", "Session"));
        }

        return (sessionId.Value, null);
    }
}
