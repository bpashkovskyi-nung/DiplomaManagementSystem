using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.Employees.Contracts;
using DiplomaManagementSystem.Application.Admin.Employees.Dtos;
using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Admin.Models;
using DiplomaManagementSystem.Web.Extensions;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Admin.Controllers;

public sealed class EmployeesController(
    IEmployeeAdminService employeeAdminService,
    IDefenceSessionService defenceSessionService,
    IValidator<EmployeeFormDto> validator) : AdminControllerBase(defenceSessionService)
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<EmployeeListItemDto> items = await employeeAdminService.GetAllAsync(cancellationToken);
        EmployeeListViewModel model = new()
        {
            Items = items.Select(MapListItem).ToList(),
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new EmployeeFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(EmployeeFormViewModel model, CancellationToken cancellationToken)
    {
        EmployeeFormDto dto = ToDto(model);
        if (!await this.TryValidateFormAsync(validator, dto, cancellationToken))
        {
            return View("Form", model);
        }

        try
        {
            await employeeAdminService.CreateAsync(dto, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        EmployeeFormDto? dto = await employeeAdminService.GetForEditAsync(id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return View("Form", new EmployeeFormViewModel
        {
            Id = dto.Id,
            FullName = dto.FullName,
            Email = dto.Email,
            AcademicRank = dto.AcademicRank,
            ShortDisplayName = dto.ShortDisplayName,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EmployeeFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        EmployeeFormDto dto = ToDto(model);
        if (!await this.TryValidateFormAsync(validator, dto, cancellationToken))
        {
            return View("Form", model);
        }

        try
        {
            await employeeAdminService.UpdateAsync(id, dto, cancellationToken);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        EmployeeDetailsDto? details = await employeeAdminService.GetDetailsAsync(id, cancellationToken);
        if (details is null)
        {
            return NotFound();
        }

        return View(new EmployeeDetailsViewModel
        {
            Id = details.Id,
            FullName = details.FullName,
            Email = details.Email,
            AcademicRankDisplay = details.AcademicRank is EmployeeAcademicRank rank
                ? AcademicRankLabels.GetDisplayName(rank)
                : "—",
            HasAssignments = details.HasAssignments,
            CreatedAt = details.CreatedAt,
        });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        EmployeeDeleteViewModel? model = await BuildDeleteViewModelAsync(id, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await employeeAdminService.DeleteAsync(id, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            EmployeeDeleteViewModel? model = await BuildDeleteViewModelAsync(id, cancellationToken);
            if (model is null)
            {
                return NotFound();
            }

            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private async Task<EmployeeDeleteViewModel?> BuildDeleteViewModelAsync(Guid id, CancellationToken cancellationToken)
    {
        EmployeeDetailsDto? details = await employeeAdminService.GetDetailsAsync(id, cancellationToken);

        return details is null
            ? null
            : new EmployeeDeleteViewModel
            {
                Id = details.Id,
                FullName = details.FullName,
                Email = details.Email,
                HasAssignments = details.HasAssignments,
            };
    }

    private static EmployeeListItemViewModel MapListItem(EmployeeListItemDto item) =>
        new()
        {
            Id = item.Id,
            FullName = item.FullName,
            Email = item.Email,
            AcademicRankDisplay = item.AcademicRank is EmployeeAcademicRank rank
                ? AcademicRankLabels.GetDisplayName(rank)
                : "—",
            CreatedAt = item.CreatedAt,
        };

    private static EmployeeFormDto ToDto(EmployeeFormViewModel model) =>
        new(model.Id, model.FullName, model.Email, model.AcademicRank, model.ShortDisplayName);
}
