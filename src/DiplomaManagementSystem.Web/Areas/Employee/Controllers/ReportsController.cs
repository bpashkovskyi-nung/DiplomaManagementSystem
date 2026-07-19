using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Web.Areas.Employee.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Areas.Employee.Controllers;

public sealed class ReportsController(IDepartmentProgressReportService progressReportService)
    : EmployeeControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Progress(Guid? sessionId, CancellationToken cancellationToken)
    {
        Guid employeeId = GetUserId();
        IReadOnlyList<(Guid SessionId, string Label)> sessions =
            await progressReportService.ListDepartmentSessionsAsync(employeeId, cancellationToken);
        Guid? selectedSessionId = sessionId ?? (sessions.Count > 0 ? sessions[0].SessionId : null);

        DepartmentProgressReportDto? report = selectedSessionId.HasValue
            ? await progressReportService.GetReportAsync(employeeId, selectedSessionId.Value, cancellationToken)
            : null;

        DepartmentProgressReportPageViewModel model = new()
        {
            SelectedSessionId = selectedSessionId,
            Sessions = sessions
                .Select(item => new SelectListItem(
                    item.Label,
                    item.SessionId.ToString(),
                    item.SessionId == selectedSessionId))
                .ToList(),
            Report = report,
        };

        return View(model);
    }
}
