using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Web.Areas.Employee.Models;
using DiplomaManagementSystem.Web.Mapping;

using FluentValidation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Employee.Controllers;

[Area("Employee")]
[Authorize(Roles = RoleNames.Employee)]
public sealed class AntiPlagiarismController(
    IAdmissionReviewService admissionReviewService,
    IValidator<CompleteCheckpointDto> completeCheckpointValidator) : EmployeeControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
    {
        IReadOnlyList<PendingCheckpointItemDto> items =
            await admissionReviewService.GetAntiPlagiarismPendingAsync(GetUserId(), cancellationToken);

        PendingCheckpointsViewModel model = new()
        {
            Title = EmployeePageTitles.AntiPlagiarism,
            Items = items.Select(EmployeeViewModelMapper.MapPendingCheckpoint).ToList(),
            NavLinks = EmployeeNavigation.HomeOnly(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Complete(CompleteCheckpointViewModel model, CancellationToken cancellationToken) =>
        CompleteCheckpointWithDocumentAsync(
            model,
            completeCheckpointValidator,
            (userId, dto, document, ct) => admissionReviewService.CompleteAntiPlagiarismAsync(userId, dto, document, ct),
            "Перевірку антиплагіату зафіксовано.",
            nameof(Pending),
            cancellationToken);
}
