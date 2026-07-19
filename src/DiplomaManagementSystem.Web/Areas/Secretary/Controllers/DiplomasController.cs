using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;
using DiplomaManagementSystem.Web.Mapping;
using DiplomaManagementSystem.Web.Secretary;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Secretary.Controllers;

public sealed class DiplomasController(
    ISecretarySessionService sessionService,
    ISecretaryAccessService accessService,
    ISecretaryDiplomaListService diplomaListService,
    ISecretaryDiplomaDetailsService diplomaDetailsService,
    ISecretaryDiplomaActionService diplomaActionService,
    IValidator<AssignReviewerDto> assignReviewerValidator,
    IValidator<AdmitDiplomaDto> admitDiplomaValidator,
    IValidator<OverrideSupervisorDto> overrideSupervisorValidator,
    IValidator<AddCommentDto> addCommentValidator,
    IValidator<OverrideAdmissionStepDto> overrideAdmissionStepValidator) : SecretaryControllerBase(sessionService, accessService)
{
    [HttpGet]
    public async Task<IActionResult> Index(
        DiplomaLifecycleStatus? lifecycleStatus,
        AdmissionStep? currentAdmissionStep,
        Guid? studyGroupId,
        string? search,
        CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        DiplomaListFilterDto filter = new(
            lifecycleStatus,
            currentAdmissionStep,
            null,
            null,
            studyGroupId,
            search);

        DiplomaListPageDto? page = await diplomaListService.GetListAsync(sessionId, filter, cancellationToken);
        if (page is null)
        {
            return RedirectToAction("Select", "Session");
        }

        return View(SecretaryListViewModelMapper.MapIndex(page, filter));
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        DiplomaDetailsDto? details = await diplomaDetailsService.GetDetailsAsync(sessionId, id, cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        return View(SecretaryDiplomaDetailsMapper.Map(details));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignReviewer(AssignReviewerViewModel model, CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        AssignReviewerDto dto = new(model.DiplomaId, model.ReviewerId);
        await ExecuteActionAsync(
            dto,
            assignReviewerValidator,
            actorId => diplomaActionService.AssignReviewerAsync(actorId, sessionId, dto, cancellationToken),
            model.DiplomaId,
            "Рецензента призначено.",
            cancellationToken);

        return RedirectToAction(nameof(Details), new { id = model.DiplomaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Admit(AdmitDiplomaViewModel model, CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        AdmitDiplomaDto dto = new(model.DiplomaId, model.DefenceDate);
        await ExecuteActionAsync(
            dto,
            admitDiplomaValidator,
            actorId => diplomaActionService.AdmitAsync(actorId, sessionId, dto, cancellationToken),
            model.DiplomaId,
            "Студента допущено до захисту.",
            cancellationToken);

        return RedirectToAction(nameof(Details), new { id = model.DiplomaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OverrideSupervisor(OverrideSupervisorViewModel model, CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        OverrideSupervisorDto dto = new(model.DiplomaId, model.SupervisorId, model.Reason);
        await ExecuteActionAsync(
            dto,
            overrideSupervisorValidator,
            actorId => diplomaActionService.OverrideSupervisorAsync(actorId, sessionId, dto, cancellationToken),
            model.DiplomaId,
            "Керівника змінено.",
            cancellationToken);

        return RedirectToAction(nameof(Details), new { id = model.DiplomaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(AddCommentViewModel model, CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        AddCommentDto dto = new(model.DiplomaId, model.Body);
        await ExecuteActionAsync(
            dto,
            addCommentValidator,
            actorId => diplomaActionService.AddCommentAsync(actorId, sessionId, dto, cancellationToken),
            model.DiplomaId,
            "Коментар додано.",
            cancellationToken);

        return RedirectToAction(nameof(Details), new { id = model.DiplomaId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> OverrideAdmissionStep(OverrideAdmissionStepViewModel model, CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        OverrideAdmissionStepDto dto = new(
            model.DiplomaId,
            model.Step,
            model.Outcome,
            model.Comment);

        await ExecuteActionAsync(
            dto,
            overrideAdmissionStepValidator,
            actorId => diplomaActionService.OverrideAdmissionStepAsync(actorId, sessionId, dto, cancellationToken),
            model.DiplomaId,
            "Крок допуску оновлено.",
            cancellationToken);

        return RedirectToAction(nameof(Details), new { id = model.DiplomaId });
    }

    private async Task ExecuteActionAsync<TDto>(
        TDto dto,
        IValidator<TDto> validator,
        Func<Guid, Task> action,
        Guid diplomaId,
        string successMessage,
        CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation = await validator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return;
        }

        try
        {
            await action(GetUserId());
            TempData["Success"] = successMessage;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }
    }
}
