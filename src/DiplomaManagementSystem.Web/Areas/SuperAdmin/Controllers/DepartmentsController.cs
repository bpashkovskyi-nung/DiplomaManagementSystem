using DiplomaManagementSystem.Application.SuperAdmin.Departments.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;
using DiplomaManagementSystem.Web.Departments;
using DiplomaManagementSystem.Web.Mapping;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin.Controllers;

public sealed class DepartmentsController(
    IDepartmentAdminService departmentAdminService,
    IFacultyAdminService facultyAdminService,
    IDepartmentSessionService departmentSessionService) : SuperAdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(Guid? facultyId, CancellationToken cancellationToken)
    {
        DepartmentListViewModel model = await BuildListViewModelAsync(facultyId, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid? facultyId, CancellationToken cancellationToken)
    {
        return View("Form", await BuildFormViewModelAsync(null, facultyId, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryValidateModel(model))
        {
            model.FacultyOptions = await LoadFacultyOptionsAsync(cancellationToken);
            return View("Form", model);
        }

        try
        {
            await departmentAdminService.CreateAsync(ToDto(model), cancellationToken);
            return RedirectToAction(nameof(Index), new { facultyId = model.FacultyId });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.FacultyOptions = await LoadFacultyOptionsAsync(cancellationToken);
            return View("Form", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        DepartmentFormDto? dto = await departmentAdminService.GetForEditAsync(id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return View("Form", await BuildFormViewModelAsync(dto, dto.FacultyId, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DepartmentFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!TryValidateModel(model))
        {
            model.FacultyOptions = await LoadFacultyOptionsAsync(cancellationToken);
            return View("Form", model);
        }

        try
        {
            await departmentAdminService.UpdateAsync(id, ToDto(model), cancellationToken);
            return RedirectToAction(nameof(Index), new { facultyId = model.FacultyId });
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            model.FacultyOptions = await LoadFacultyOptionsAsync(cancellationToken);
            return View("Form", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, Guid facultyId, CancellationToken cancellationToken)
    {
        try
        {
            await departmentAdminService.DeactivateAsync(id, cancellationToken);
            return RedirectToAction(nameof(Index), new { facultyId });
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index), new { facultyId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(Guid id, CancellationToken cancellationToken)
    {
        DepartmentFormDto? department = await departmentAdminService.GetForEditAsync(id, cancellationToken);
        if (department is null)
        {
            return NotFound();
        }

        departmentSessionService.SetSelectedDepartment(HttpContext, id, superAdminImpersonating: true);
        return RedirectToAction("Index", "Home", new { area = "Admin" });
    }

    private async Task<DepartmentListViewModel> BuildListViewModelAsync(
        Guid? facultyId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentListItemDto> items =
            await departmentAdminService.GetAllAsync(facultyId, cancellationToken);

        return new DepartmentListViewModel
        {
            SelectedFacultyId = facultyId,
            FacultyOptions = await LoadFacultyOptionsAsync(cancellationToken),
            Items = items.Select(SuperAdminViewModelMapper.Map).ToList(),
        };
    }

    private async Task<DepartmentFormViewModel> BuildFormViewModelAsync(
        DepartmentFormDto? dto,
        Guid? facultyId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FacultyOptionViewModel> facultyOptions = await LoadFacultyOptionsAsync(cancellationToken);
        Guid selectedFacultyId = dto?.FacultyId ?? facultyId ?? facultyOptions.FirstOrDefault()?.Id ?? Guid.Empty;

        return dto is null
            ? new DepartmentFormViewModel
            {
                FacultyId = selectedFacultyId,
                FacultyOptions = facultyOptions,
            }
            : new DepartmentFormViewModel
            {
                Id = dto.Id,
                FacultyId = dto.FacultyId,
                Name = dto.Name,
                SpecialtyCode = dto.SpecialtyCode,
                SpecialtyName = dto.SpecialtyName,
                StudyForm = dto.StudyForm,
                FacultyOptions = facultyOptions,
            };
    }

    private async Task<IReadOnlyList<FacultyOptionViewModel>> LoadFacultyOptionsAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FacultyListItemDto> faculties =
            await facultyAdminService.GetAllAsync(cancellationToken);

        return faculties
            .Where(faculty => faculty.IsActive)
            .Select(SuperAdminViewModelMapper.MapOption)
            .ToList();
    }

    private static DepartmentFormDto ToDto(DepartmentFormViewModel model) =>
        new(
            model.Id,
            model.FacultyId,
            model.Name.Trim(),
            model.SpecialtyCode.Trim(),
            model.SpecialtyName.Trim(),
            model.StudyForm.Trim());
}
