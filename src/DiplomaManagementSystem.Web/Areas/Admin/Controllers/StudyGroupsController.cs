using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.StudyGroups.Contracts;
using DiplomaManagementSystem.Application.Admin.StudyGroups.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Admin.Models;
using DiplomaManagementSystem.Web.Extensions;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Admin.Controllers;

public sealed class StudyGroupsController(
    IStudyGroupAdminService studyGroupAdminService,
    IDefenceSessionService defenceSessionService,
    IValidator<StudyGroupFormDto> validator) : AdminControllerBase(defenceSessionService)
{
    [HttpGet]
    public async Task<IActionResult> Create(Guid defenceSessionId, CancellationToken cancellationToken)
    {
        string? sessionLabel = await GetSessionLabelAsync(defenceSessionId, cancellationToken);
        if (sessionLabel is null)
        {
            return NotFound();
        }

        return View("Form", new StudyGroupFormViewModel
        {
            DefenceSessionId = defenceSessionId,
            SessionLabel = sessionLabel,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudyGroupFormViewModel model, CancellationToken cancellationToken)
    {
        StudyGroupFormDto dto = ToDto(model);
        if (!await this.TryValidateFormAsync(validator, dto, cancellationToken))
        {
            model.SessionLabel = await GetSessionLabelAsync(model.DefenceSessionId, cancellationToken) ?? model.SessionLabel;
            return View("Form", model);
        }

        try
        {
            await studyGroupAdminService.CreateAsync(dto, cancellationToken);
            return RedirectToSessionDetails(model.DefenceSessionId);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.SessionLabel = await GetSessionLabelAsync(model.DefenceSessionId, cancellationToken) ?? model.SessionLabel;
            return View("Form", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        StudyGroupFormViewModel? model = await BuildFormViewModelAsync(id, cancellationToken);
        return model is null ? NotFound() : View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, StudyGroupFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        StudyGroupFormDto dto = ToDto(model);
        if (!await this.TryValidateFormAsync(validator, dto, cancellationToken))
        {
            model.SessionLabel = await GetSessionLabelAsync(model.DefenceSessionId, cancellationToken) ?? model.SessionLabel;
            return View("Form", model);
        }

        try
        {
            await studyGroupAdminService.UpdateAsync(id, dto, cancellationToken);
            return RedirectToSessionDetails(model.DefenceSessionId);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.SessionLabel = await GetSessionLabelAsync(model.DefenceSessionId, cancellationToken) ?? model.SessionLabel;
            return View("Form", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        StudyGroupDeleteViewModel? model = await BuildDeleteViewModelAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        StudyGroupDeleteViewModel? existing = await BuildDeleteViewModelAsync(id, cancellationToken);
        if (existing is null)
        {
            return NotFound();
        }

        try
        {
            await studyGroupAdminService.DeleteAsync(id, cancellationToken);
            return RedirectToSessionDetails(existing.DefenceSessionId);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(existing);
        }
    }

    private async Task<StudyGroupFormViewModel?> BuildFormViewModelAsync(Guid id, CancellationToken cancellationToken)
    {
        StudyGroupFormDto? dto = await studyGroupAdminService.GetForEditAsync(id, cancellationToken);
        if (dto is null)
        {
            return null;
        }

        string? sessionLabel = dto.SessionLabel;
        if (sessionLabel is null)
        {
            sessionLabel = await GetSessionLabelAsync(dto.DefenceSessionId, cancellationToken);
            if (sessionLabel is null)
            {
                return null;
            }
        }

        return new StudyGroupFormViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
            Course = dto.Course,
            DefenceSessionId = dto.DefenceSessionId,
            SessionLabel = sessionLabel,
        };
    }

    private async Task<StudyGroupDeleteViewModel?> BuildDeleteViewModelAsync(Guid id, CancellationToken cancellationToken)
    {
        StudyGroupListItemDto? item = await studyGroupAdminService.GetListItemAsync(id, cancellationToken);

        return item is null
            ? null
            : new StudyGroupDeleteViewModel
            {
                Id = item.Id,
                Name = item.Name,
                DefenceSessionId = item.DefenceSessionId,
                SessionLabel = item.SessionLabel,
                StudentCount = item.StudentCount,
            };
    }

    private RedirectToActionResult RedirectToSessionDetails(Guid defenceSessionId) =>
        RedirectToAction("Details", "DefenceSessions", new { id = defenceSessionId });

    private static StudyGroupFormDto ToDto(StudyGroupFormViewModel model) =>
        new(model.Id, model.DefenceSessionId, model.Name, model.Course);
}
