using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Secretary;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Secretary.Controllers;

public sealed class SessionSetupController(
    ISecretarySessionService sessionService,
    ISecretaryAccessService accessService,
    ISessionSetupService sessionSetupService,
    IValidator<SaveMilestonesDto> milestonesValidator,
    IValidator<SaveDefenceDatesDto> defenceDatesValidator) : SecretaryControllerBase(sessionService, accessService)
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        SessionSetupPageDto? page = await sessionSetupService.GetPageAsync(sessionId, cancellationToken);
        return page is null ? RedirectToAction("Select", "Session") : View(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SaveMilestones(
        SaveMilestonesDto request,
        CancellationToken cancellationToken) =>
        SaveAsync(
            request,
            milestonesValidator,
            (sessionId, dto) => sessionSetupService.SaveMilestonesAsync(sessionId, dto, cancellationToken),
            "Етапи прогресу збережено.",
            cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> SaveDefenceDates(
        IReadOnlyList<DateOnly?> dates,
        CancellationToken cancellationToken)
    {
        SaveDefenceDatesDto request = new(dates.Where(date => date.HasValue).Select(date => date!.Value).ToList());
        return
        SaveAsync(
            request,
            defenceDatesValidator,
            (sessionId, dto) => sessionSetupService.SaveDefenceDatesAsync(sessionId, dto, cancellationToken),
            "Доступні дати захисту збережено.",
            cancellationToken);
    }

    private async Task<IActionResult> SaveAsync<TDto>(
        TDto request,
        IValidator<TDto> validator,
        Func<Guid, TDto, Task> save,
        string successMessage,
        CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        FluentValidation.Results.ValidationResult validation =
            await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await save(sessionId, request);
            TempData["Success"] = successMessage;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
