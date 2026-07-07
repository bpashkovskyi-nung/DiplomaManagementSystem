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
public sealed class FormattingReviewController(
    IAdmissionReviewService admissionReviewService,
    IValidator<CompleteCheckpointDto> completeCheckpointValidator) : EmployeeControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Pending(CancellationToken cancellationToken)
    {
        IReadOnlyList<PendingCheckpointItemDto> items =
            await admissionReviewService.GetFormattingReviewPendingAsync(GetUserId(), cancellationToken);

        PendingCheckpointsViewModel model = new()
        {
            Title = EmployeePageTitles.FormattingReview,
            Items = items.Select(EmployeeViewModelMapper.MapPendingCheckpoint).ToList(),
            RequiresDocumentFile = false,
            NavLinks = EmployeeNavigation.HomeOnly(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Complete(CompleteCheckpointViewModel model, CancellationToken cancellationToken) =>
        CompleteCheckpointAsync(
            model,
            completeCheckpointValidator,
            (userId, dto, ct) => admissionReviewService.CompleteFormattingReviewAsync(userId, dto, ct),
            "Нормоконтроль зафіксовано.",
            nameof(Pending),
            cancellationToken);
}
