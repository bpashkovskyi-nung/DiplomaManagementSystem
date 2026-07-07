using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Contracts;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Admin.Models;
using DiplomaManagementSystem.Web.Extensions;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Admin.Controllers;

public sealed class EmployeeWorkloadLimitsController(
    IEmployeeWorkloadLimitAdminService workloadLimitAdminService,
    IDefenceSessionService defenceSessionService,
    IValidator<SetEmployeeWorkloadLimitDto> validator) : AdminControllerBase(defenceSessionService)
{
    [HttpGet]
    public async Task<IActionResult> Index(Guid defenceSessionId, CancellationToken cancellationToken)
    {
        EmployeeWorkloadLimitsViewModel? model = await BuildViewModelAsync(defenceSessionId, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLimit(SetEmployeeWorkloadLimitFormViewModel model, CancellationToken cancellationToken)
    {
        SetEmployeeWorkloadLimitDto dto = new(
            model.DefenceSessionId,
            model.EmployeeId,
            model.MaxSupervisorStudents,
            model.MaxReviewerStudents);

        if (!await this.TryValidateFormAsync(validator, dto, cancellationToken))
        {
            EmployeeWorkloadLimitsViewModel? viewModel = await BuildViewModelAsync(model.DefenceSessionId, cancellationToken);
            return viewModel is null ? NotFound() : View("Index", viewModel);
        }

        try
        {
            await workloadLimitAdminService.SetLimitAsync(dto, cancellationToken);
            ViewData["Success"] = "Ліміти збережено.";
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
        }

        EmployeeWorkloadLimitsViewModel? result = await BuildViewModelAsync(model.DefenceSessionId, cancellationToken);
        return result is null ? NotFound() : View("Index", result);
    }

    private async Task<EmployeeWorkloadLimitsViewModel?> BuildViewModelAsync(
        Guid defenceSessionId,
        CancellationToken cancellationToken)
    {
        EmployeeWorkloadLimitsPageDto? page = await workloadLimitAdminService.GetPageAsync(
            defenceSessionId,
            cancellationToken);

        if (page is null)
        {
            return null;
        }

        return new EmployeeWorkloadLimitsViewModel
        {
            DefenceSessionId = page.DefenceSessionId,
            SessionLabel = page.SessionLabel,
            Rows = page.Rows.Select(row => new EmployeeWorkloadLimitRowViewModel
            {
                EmployeeId = row.EmployeeId,
                FullName = row.FullName,
                Email = row.Email,
                MaxSupervisorStudents = row.MaxSupervisorStudents,
                MaxReviewerStudents = row.MaxReviewerStudents,
                ConfirmedSupervisorCount = row.ConfirmedSupervisorCount,
                ReviewerAssignmentCount = row.ReviewerAssignmentCount,
            }).ToList(),
        };
    }
}
