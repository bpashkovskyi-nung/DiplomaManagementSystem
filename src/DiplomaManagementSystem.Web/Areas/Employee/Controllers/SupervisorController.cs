using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Employee.Models;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;
using DiplomaManagementSystem.Web.Mapping;

using FluentValidation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Employee.Controllers;

[Area("Employee")]
[Authorize(Roles = RoleNames.Employee)]
public sealed class SupervisorController(
    ISupervisorWorkflowService supervisorWorkflowService,
    ISupervisorDiplomaListService supervisorDiplomaListService,
    ISupervisorDiplomaDetailsService supervisorDiplomaDetailsService,
    ISupervisorProgressService supervisorProgressService,
    IDefenceDateRequestService defenceDateRequestService,
    IAdmissionReviewService admissionReviewService,
    IValidator<SupervisorActionDto> supervisorRejectValidator,
    IValidator<ApproveTopicDto> approveTopicValidator,
    IValidator<ReviewTopicDto> reviewTopicRejectValidator,
    IValidator<CompleteCheckpointDto> completeCheckpointValidator,
    IValidator<SetMilestoneProgressDto> milestoneProgressValidator,
    IValidator<RequestDefenceDateDto> defenceDateRequestValidator) : EmployeeControllerBase
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

        SupervisorDiplomaListPageDto page = await supervisorDiplomaListService.GetListAsync(
            GetUserId(),
            filter,
            cancellationToken);

        SupervisorStudentsListViewModel model = SecretaryListViewModelMapper.MapSupervisorStudents(
            page,
            filter,
            showSupervisorColumn: false,
            showDetailsLink: true);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        DiplomaDetailsDto? details = await supervisorDiplomaDetailsService.GetDetailsAsync(
            GetUserId(),
            id,
            cancellationToken);

        if (details is null)
        {
            return NotFound();
        }

        DiplomaDetailsViewModel model = SecretaryDiplomaDetailsMapper.Map(details);
        model.DefenceDateRequest = await defenceDateRequestService.GetFormForSupervisorAsync(
            GetUserId(),
            id,
            cancellationToken);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Progress(Guid? sessionId, CancellationToken cancellationToken)
    {
        SupervisorProgressPageDto page =
            await supervisorProgressService.GetPageAsync(GetUserId(), sessionId, cancellationToken);
        return View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetProgress(
        SetMilestoneProgressDto request,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation =
            await milestoneProgressValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(Progress), new { sessionId });
        }

        try
        {
            await supervisorProgressService.SetActualPercentAsync(GetUserId(), request, cancellationToken);
            TempData["Success"] = "Прогрес оновлено.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Progress), new { sessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestDefenceDate(
        RequestDefenceDateDto request,
        CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation =
            await defenceDateRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(Details), new { id = request.DiplomaId });
        }

        try
        {
            await defenceDateRequestService.RequestAsSupervisorAsync(GetUserId(), request, cancellationToken);
            TempData["Success"] = "Побажання щодо дати захисту надіслано.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id = request.DiplomaId });
    }

    [HttpGet]
    public async Task<IActionResult> PendingStudents(CancellationToken cancellationToken)
    {
        IReadOnlyList<PendingStudentDto> items =
            await supervisorWorkflowService.GetPendingStudentsAsync(GetUserId(), cancellationToken);

        PendingStudentsViewModel model = new()
        {
            Items = items.Select(EmployeeViewModelMapper.MapPendingStudent).ToList(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(Guid diplomaId, CancellationToken cancellationToken)
    {
        try
        {
            await supervisorWorkflowService.ConfirmStudentAsync(GetUserId(), diplomaId, cancellationToken);
            TempData["Success"] = "Студента підтверджено як керівника.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(PendingStudents));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(SupervisorActionViewModel model, CancellationToken cancellationToken)
    {
        SupervisorActionDto dto = new(model.DiplomaId, model.Comment);
        FluentValidation.Results.ValidationResult validation =
            await supervisorRejectValidator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(PendingStudents));
        }

        try
        {
            await supervisorWorkflowService.RejectStudentAsync(GetUserId(), dto, cancellationToken);
            TempData["Success"] = "Запит студента відхилено.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(PendingStudents));
    }

    [HttpGet]
    public async Task<IActionResult> TopicReviews(CancellationToken cancellationToken)
    {
        IReadOnlyList<TopicReviewItemDto> items =
            await supervisorWorkflowService.GetTopicReviewsAsync(GetUserId(), cancellationToken);

        TopicReviewsViewModel model = new()
        {
            Items = items.Select(EmployeeViewModelMapper.MapTopicReviewItem).ToList(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ApproveTopic(ApproveTopicViewModel model, CancellationToken cancellationToken) =>
        ApproveTopicAsync(
            model,
            approveTopicValidator,
            supervisorWorkflowService.ApproveTopicAsync,
            "Тему передано на розгляд завідувачу кафедри.",
            nameof(TopicReviews),
            cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RejectTopic(ReviewTopicViewModel model, CancellationToken cancellationToken) =>
        RejectTopicAsync(
            model,
            reviewTopicRejectValidator,
            supervisorWorkflowService.RejectTopicAsync,
            "Тему відхилено.",
            nameof(TopicReviews),
            cancellationToken);

    [HttpGet]
    public async Task<IActionResult> Checkpoints(CancellationToken cancellationToken)
    {
        IReadOnlyList<PendingCheckpointItemDto> items =
            await admissionReviewService.GetSupervisorFeedbackPendingAsync(GetUserId(), cancellationToken);

        PendingCheckpointsViewModel model = new()
        {
            Title = EmployeePageTitles.SubmitSupervisorFeedback,
            Items = items.Select(EmployeeViewModelMapper.MapPendingCheckpoint).ToList(),
            FormAction = "CompleteCheckpoint",
            RoleArea = EmployeeRoleArea.Supervisor,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CompleteCheckpoint(CompleteCheckpointViewModel model, CancellationToken cancellationToken) =>
        CompleteCheckpointWithDocumentAsync(
            model,
            completeCheckpointValidator,
            (userId, dto, document, ct) => admissionReviewService.CompleteSupervisorFeedbackAsync(userId, dto, document, ct),
            "Відгук надіслано.",
            nameof(Checkpoints),
            cancellationToken);
}
