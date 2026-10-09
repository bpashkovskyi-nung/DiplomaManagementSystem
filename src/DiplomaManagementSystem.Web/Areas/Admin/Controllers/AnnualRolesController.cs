using DiplomaManagementSystem.Application.Admin.AnnualRoles.Contracts;
using DiplomaManagementSystem.Application.Admin.AnnualRoles.Dtos;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.ExaminationCommission.Contracts;
using DiplomaManagementSystem.Application.Admin.ExaminationCommission.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Admin.Models;
using DiplomaManagementSystem.Web.Extensions;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Areas.Admin.Controllers;

public sealed class AnnualRolesController(
    IAnnualRoleService annualRoleService,
    IExaminationCommissionService examinationCommissionService,
    IDefenceSessionService defenceSessionService,
    IValidator<AssignAnnualRoleDto> assignValidator,
    IValidator<SaveExaminationCommissionDto> commissionValidator) : AdminControllerBase(defenceSessionService)
{
    private const int DefaultMemberSlots = 3;

    [HttpGet]
    public async Task<IActionResult> Index(Guid defenceSessionId, CancellationToken cancellationToken)
    {
        AnnualRolesViewModel? model = await BuildIndexViewModelAsync(
            defenceSessionId,
            assignForm: null,
            commissionForm: null,
            cancellationToken);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(AssignAnnualRoleFormViewModel model, CancellationToken cancellationToken)
    {
        AssignAnnualRoleDto dto = new(model.DefenceSessionId, model.RoleType, model.EmployeeId);
        if (!await this.TryValidateFormAsync(assignValidator, dto, cancellationToken))
        {
            AnnualRolesViewModel? viewModel = await BuildIndexViewModelAsync(
                model.DefenceSessionId,
                model,
                commissionForm: null,
                cancellationToken);
            return viewModel is null ? NotFound() : View("Index", viewModel);
        }

        try
        {
            await annualRoleService.AssignAsync(dto, cancellationToken);
            AnnualRolesViewModel? viewModel = await BuildIndexViewModelAsync(
                model.DefenceSessionId,
                null,
                null,
                cancellationToken);
            if (viewModel is null)
            {
                return NotFound();
            }

            ViewData["Success"] = "Роль призначено.";
            return View("Index", viewModel);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            AnnualRolesViewModel? viewModel = await BuildIndexViewModelAsync(
                model.DefenceSessionId,
                model,
                null,
                cancellationToken);
            return viewModel is null ? NotFound() : View("Index", viewModel);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCommission(
        ExaminationCommissionFormViewModel model,
        CancellationToken cancellationToken)
    {
        SaveExaminationCommissionDto dto = ToSaveDto(model);
        if (!await this.TryValidateFormAsync(commissionValidator, dto, cancellationToken))
        {
            AnnualRolesViewModel? invalidView = await BuildIndexViewModelAsync(
                model.DefenceSessionId,
                assignForm: null,
                commissionForm: model,
                cancellationToken);
            return invalidView is null ? NotFound() : View("Index", invalidView);
        }

        try
        {
            await examinationCommissionService.SaveAsync(dto, cancellationToken);
            AnnualRolesViewModel? viewModel = await BuildIndexViewModelAsync(
                model.DefenceSessionId,
                null,
                null,
                cancellationToken);
            if (viewModel is null)
            {
                return NotFound();
            }

            ViewData["Success"] = "Склад ЕК збережено.";
            return View("Index", viewModel);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            AnnualRolesViewModel? viewModel = await BuildIndexViewModelAsync(
                model.DefenceSessionId,
                null,
                model,
                cancellationToken);
            return viewModel is null ? NotFound() : View("Index", viewModel);
        }
    }

    private async Task<AnnualRolesViewModel?> BuildIndexViewModelAsync(
        Guid defenceSessionId,
        AssignAnnualRoleFormViewModel? assignForm,
        ExaminationCommissionFormViewModel? commissionForm,
        CancellationToken cancellationToken)
    {
        AnnualRolesPageDto? page = await annualRoleService.GetPageAsync(defenceSessionId, cancellationToken);
        if (page is null)
        {
            return null;
        }

        ExaminationCommissionEditorDto? commissionEditor =
            await examinationCommissionService.GetEditorAsync(defenceSessionId, cancellationToken);

        AnnualRolesViewModel model = new()
        {
            DefenceSessionId = page.DefenceSessionId,
            SessionLabel = page.SessionLabel,
            Employees = page.Employees
                .Select(employee => new SelectListItem($"{employee.FullName} ({employee.Email})", employee.Id.ToString()))
                .ToList(),
            Roles = page.Roles.Select(role => new AnnualRoleSlotViewModel
            {
                RoleType = role.RoleType,
                RoleDisplay = UkrainianDisplay.FormatAnnualRoleType(role.RoleType),
                AssignedEmployeeId = role.AssignedEmployeeId,
                AssignedEmployeeName = role.AssignedEmployeeName,
                SelectedEmployeeId = role.AssignedEmployeeId ?? Guid.Empty,
            }).ToList(),
            CommissionEmployees = (commissionEditor?.Employees ?? [])
                .Select(employee => new CommissionEmployeeOptionViewModel
                {
                    Id = employee.Id,
                    FullName = employee.FullName,
                    Email = employee.Email,
                    Position = employee.Position,
                })
                .ToList(),
            Commission = commissionForm ?? BuildCommissionForm(defenceSessionId, commissionEditor?.Commission),
        };

        if (assignForm is not null)
        {
            AnnualRoleSlotViewModel? slot = model.Roles.FirstOrDefault(role => role.RoleType == assignForm.RoleType);
            if (slot is not null)
            {
                slot.SelectedEmployeeId = assignForm.EmployeeId;
            }
        }

        return model;
    }

    private static ExaminationCommissionFormViewModel BuildCommissionForm(
        Guid defenceSessionId,
        ExaminationCommissionDto? commission)
    {
        ExaminationCommissionFormViewModel form = new()
        {
            DefenceSessionId = defenceSessionId,
            Chair = commission?.Chair is ExaminationCommissionParticipantDto chair
                ? ToParticipantForm(chair)
                : new ExaminationCommissionParticipantFormViewModel(),
            Members = (commission?.Members ?? [])
                .Select(ToParticipantForm)
                .ToList(),
        };

        while (form.Members.Count < DefaultMemberSlots)
        {
            form.Members.Add(new ExaminationCommissionParticipantFormViewModel());
        }

        return form;
    }

    private static ExaminationCommissionParticipantFormViewModel ToParticipantForm(
        ExaminationCommissionParticipantDto dto) =>
        new()
        {
            IsExternal = dto.EmployeeId is null,
            EmployeeId = dto.EmployeeId,
            FullName = dto.FullName,
            Position = dto.Position,
        };

    private static SaveExaminationCommissionDto ToSaveDto(ExaminationCommissionFormViewModel model) =>
        new(
            model.DefenceSessionId,
            ToSaveParticipant(model.Chair),
            model.Members.Select(ToSaveParticipant).ToList());

    private static SaveExaminationCommissionParticipantDto ToSaveParticipant(
        ExaminationCommissionParticipantFormViewModel model) =>
        new(model.IsExternal, model.EmployeeId, model.FullName, model.Position);
}
