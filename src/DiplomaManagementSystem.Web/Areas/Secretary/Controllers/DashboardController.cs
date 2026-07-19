using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Web.Mapping;
using DiplomaManagementSystem.Web.Secretary;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Secretary.Controllers;

public sealed class DashboardController(
    ISecretarySessionService sessionService,
    ISecretaryAccessService accessService,
    ISecretaryDashboardService dashboardService) : SecretaryControllerBase(sessionService, accessService)
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        SecretaryDashboardDto? dashboard = await dashboardService.GetDashboardAsync(sessionId, cancellationToken);
        if (dashboard is null)
        {
            return RedirectToAction("Select", "Session");
        }

        return View(SecretaryDashboardViewModelMapper.Map(dashboard));
    }
}
