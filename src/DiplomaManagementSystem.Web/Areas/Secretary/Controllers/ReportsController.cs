using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Web.Mapping;
using DiplomaManagementSystem.Web.Secretary;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Secretary.Controllers;

public sealed class ReportsController(
    ISecretarySessionService sessionService,
    ISecretaryAccessService accessService,
    IAdmittedReportService admittedReportService) : SecretaryControllerBase(sessionService, accessService)
{
    [HttpGet]
    public async Task<IActionResult> Admitted(CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        AdmittedReportDto? report = await admittedReportService.GetReportAsync(sessionId, cancellationToken);
        if (report is null)
        {
            return RedirectToAction("Select", "Session");
        }

        return View(SecretaryReportsViewModelMapper.Map(report));
    }

    [HttpGet]
    public async Task<IActionResult> AdmittedCsv(CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        byte[] content = await admittedReportService.ExportCsvAsync(sessionId, cancellationToken);
        return File(content, "text/csv", "admitted-report.csv");
    }
}
