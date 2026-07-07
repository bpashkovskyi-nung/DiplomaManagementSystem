using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Areas.Employee.Models;
using DiplomaManagementSystem.Web.Mapping;

using FluentValidation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Employee.Controllers;

[Area("Employee")]
[Authorize(Roles = RoleNames.Employee)]
public sealed class ReviewerController(
    IAdmissionReviewService admissionReviewService,
    IReviewerDiplomaListService reviewerDiplomaListService,
    IValidator<CompleteCheckpointDto> completeCheckpointValidator) : EmployeeControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Students(
        DiplomaLifecycleStatus? lifecycleStatus,
        AdmissionStep? currentAdmissionStep,
        Guid? studyGroupId,
        string? search,
        CancellationToken cancellationToken)
    {
        DiplomaListFilterDto filter = new(
            lifecycleStatus,
            currentAdmissionStep,
            null,
            null,
            studyGroupId,
            search);

        ReviewerDiplomaListPageDto page = await reviewerDiplomaListService.GetListAsync(
            GetUserId(),
            filter,
            cancellationToken);

        SupervisorStudentsListViewModel model = SecretaryListViewModelMapper.MapReviewerStudents(
            page,
            filter,
            showSupervisorColumn: true,
            showDetailsLink: false);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Assignments(CancellationToken cancellationToken)
    {
        IReadOnlyList<ReviewerAssignmentItemDto> items =
            await admissionReviewService.GetReviewerAssignmentsAsync(GetUserId(), cancellationToken);

        PendingCheckpointsViewModel model = new()
        {
            Title = EmployeePageTitles.SubmitExternalReview,
            Items = items.Select(EmployeeViewModelMapper.MapReviewerAssignment).ToList(),
            NavLinks = EmployeeNavigation.ReviewerWorkflow(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Complete(CompleteCheckpointViewModel model, CancellationToken cancellationToken) =>
        CompleteCheckpointWithDocumentAsync(
            model,
            completeCheckpointValidator,
            (userId, dto, document, ct) => admissionReviewService.CompleteExternalReviewAsync(userId, dto, document, ct),
            "Рецензію зафіксовано.",
            nameof(Assignments),
            cancellationToken);
}
