using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;
using DiplomaManagementSystem.Web.Mapping;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin.Controllers;

public sealed class DepartmentAdminsController(
    IDepartmentAdminAssignmentService assignmentService,
    IDepartmentAdminService departmentAdminService) : SuperAdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(Guid? departmentId, CancellationToken cancellationToken)
    {
        DepartmentAdminListViewModel model = await BuildListViewModelAsync(departmentId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(DepartmentAdminListViewModel model, CancellationToken cancellationToken)
    {
        if (model.SelectedDepartmentId is not Guid departmentId)
        {
            ModelState.AddModelError(string.Empty, "Оберіть кафедру.");
            return View("Index", await BuildListViewModelAsync(null, cancellationToken));
        }

        if (string.IsNullOrWhiteSpace(model.AssignEmail))
        {
            ModelState.AddModelError(nameof(model.AssignEmail), "Вкажіть email адміністратора.");
            DepartmentAdminListViewModel page = await BuildListViewModelAsync(departmentId, cancellationToken);
            page.AssignEmail = model.AssignEmail;
            return View("Index", page);
        }

        try
        {
            await assignmentService.AssignAsync(
                new DepartmentAdminAssignDto(departmentId, model.AssignEmail.Trim()),
                cancellationToken);
            TempData["Success"] = "Адміністратора призначено.";
            return RedirectToAction(nameof(Index), new { departmentId });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            DepartmentAdminListViewModel page = await BuildListViewModelAsync(departmentId, cancellationToken);
            page.AssignEmail = model.AssignEmail;
            return View("Index", page);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid assignmentId, Guid departmentId, CancellationToken cancellationToken)
    {
        try
        {
            await assignmentService.RemoveAsync(assignmentId, cancellationToken);
            TempData["Success"] = "Призначення знято.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { departmentId });
    }

    private async Task<DepartmentAdminListViewModel> BuildListViewModelAsync(
        Guid? departmentId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentListItemDto> departments =
            await departmentAdminService.GetAllAsync(cancellationToken: cancellationToken);

        List<DepartmentOptionViewModel> departmentOptions = departments
            .Where(department => department.IsActive)
            .Select(SuperAdminViewModelMapper.MapDepartmentOption)
            .ToList();

        Guid? selectedDepartmentId = departmentId ?? departmentOptions.FirstOrDefault()?.Id;
        DepartmentAdminListViewModel model = new()
        {
            SelectedDepartmentId = selectedDepartmentId,
            DepartmentOptions = departmentOptions,
        };

        if (selectedDepartmentId is Guid id)
        {
            model.SelectedDepartmentName = departmentOptions
                .FirstOrDefault(option => option.Id == id)
                ?.Label;
            IReadOnlyList<DepartmentAdminListItemDto> items =
                await assignmentService.GetByDepartmentAsync(id, cancellationToken);
            model.Items = items.Select(SuperAdminViewModelMapper.Map).ToList();
        }

        return model;
    }
}
