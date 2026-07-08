using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.Specialties.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Specialties.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;
using DiplomaManagementSystem.Web.Departments;
using DiplomaManagementSystem.Web.Mapping;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin.Controllers;

public sealed class DepartmentsController(
    IDepartmentAdminService departmentAdminService,
    IDepartmentAdminAssignmentService assignmentService,
    IFacultyAdminService facultyAdminService,
    ISpecialtyAdminService specialtyAdminService,
    IDepartmentSessionService departmentSessionService) : SuperAdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(Guid? facultyId, CancellationToken cancellationToken)
    {
        if (facultyId is not Guid id)
        {
            return RedirectToAction("Index", "Faculties");
        }

        FacultyFormDto? faculty = await facultyAdminService.GetForEditAsync(id, cancellationToken);
        if (faculty is null)
        {
            return NotFound();
        }

        DepartmentListViewModel model = await BuildListViewModelAsync(id, faculty.Name, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid facultyId, CancellationToken cancellationToken)
    {
        FacultyFormDto? faculty = await facultyAdminService.GetForEditAsync(facultyId, cancellationToken);
        if (faculty is null)
        {
            return NotFound();
        }

        return View("Form", await BuildFormViewModelAsync(null, facultyId, faculty.Name, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DepartmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryValidateModel(model))
        {
            model.FacultyOptions = await LoadFacultyOptionsAsync(cancellationToken);
            model.FacultyName = await ResolveFacultyNameAsync(model.FacultyId, cancellationToken);
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
            model.FacultyName = await ResolveFacultyNameAsync(model.FacultyId, cancellationToken);
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

        FacultyFormDto? faculty = await facultyAdminService.GetForEditAsync(dto.FacultyId, cancellationToken);
        if (faculty is null)
        {
            return NotFound();
        }

        return View("Form", await BuildFormViewModelAsync(dto, dto.FacultyId, faculty.Name, cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, DepartmentFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!TryValidateModel(model))
        {
            model.FacultyOptions = await LoadFacultyOptionsAsync(cancellationToken);
            model.FacultyName = await ResolveFacultyNameAsync(model.FacultyId, cancellationToken);
            model.Admins = await LoadAdminsAsync(id, cancellationToken);
            model.EmployeeOptions = await LoadEmployeeOptionsAsync(id, cancellationToken);
            model.Specialties = await LoadSpecialtiesAsync(id, cancellationToken);
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
            model.FacultyName = await ResolveFacultyNameAsync(model.FacultyId, cancellationToken);
            model.Admins = await LoadAdminsAsync(id, cancellationToken);
            model.EmployeeOptions = await LoadEmployeeOptionsAsync(id, cancellationToken);
            model.Specialties = await LoadSpecialtiesAsync(id, cancellationToken);
            return View("Form", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSpecialty(
        Guid departmentId,
        Guid facultyId,
        string code,
        string name,
        CancellationToken cancellationToken)
    {
        try
        {
            await specialtyAdminService.CreateAsync(
                new SpecialtyFormDto(null, departmentId, code, name),
                cancellationToken);
            TempData["Success"] = "Спеціальність додано.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id = departmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateSpecialty(
        Guid specialtyId,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        try
        {
            await specialtyAdminService.DeactivateAsync(specialtyId, cancellationToken);
            TempData["Success"] = "Спеціальність деактивовано.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id = departmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignAdmin(
        Guid departmentId,
        Guid facultyId,
        Guid assignUserId,
        CancellationToken cancellationToken)
    {
        try
        {
            await assignmentService.AssignAsync(
                new DepartmentAdminAssignDto(departmentId, assignUserId),
                cancellationToken);
            TempData["Success"] = "Адміністратора призначено.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Edit), new { id = departmentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveAdmin(
        Guid assignmentId,
        Guid departmentId,
        CancellationToken cancellationToken)
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

        return RedirectToAction(nameof(Edit), new { id = departmentId });
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
        Guid facultyId,
        string facultyName,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentListItemDto> items =
            await departmentAdminService.GetAllAsync(facultyId, cancellationToken);

        return new DepartmentListViewModel
        {
            FacultyId = facultyId,
            FacultyName = facultyName,
            Items = items.Select(SuperAdminViewModelMapper.Map).ToList(),
        };
    }

    private async Task<DepartmentFormViewModel> BuildFormViewModelAsync(
        DepartmentFormDto? dto,
        Guid facultyId,
        string facultyName,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<FacultyOptionViewModel> facultyOptions = await LoadFacultyOptionsAsync(cancellationToken);
        Guid selectedFacultyId = dto?.FacultyId ?? facultyId;

        DepartmentFormViewModel model = dto is null
            ? new DepartmentFormViewModel
            {
                FacultyId = selectedFacultyId,
                FacultyName = facultyName,
                FacultyOptions = facultyOptions,
            }
            : new DepartmentFormViewModel
            {
                Id = dto.Id,
                FacultyId = dto.FacultyId,
                FacultyName = facultyName,
                Name = dto.Name,
                FacultyOptions = facultyOptions,
            };

        if (dto?.Id is Guid departmentId)
        {
            model.Admins = await LoadAdminsAsync(departmentId, cancellationToken);
            model.EmployeeOptions = await LoadEmployeeOptionsAsync(departmentId, cancellationToken);
            model.Specialties = await LoadSpecialtiesAsync(departmentId, cancellationToken);
        }

        return model;
    }

    private async Task<IReadOnlyList<SpecialtyListItemViewModel>> LoadSpecialtiesAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SpecialtyListItemDto> items =
            await specialtyAdminService.GetByDepartmentAsync(departmentId, cancellationToken);

        return items.Select(SuperAdminViewModelMapper.Map).ToList();
    }

    private async Task<IReadOnlyList<DepartmentAdminListItemViewModel>> LoadAdminsAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentAdminListItemDto> items =
            await assignmentService.GetByDepartmentAsync(departmentId, cancellationToken);

        return items.Select(SuperAdminViewModelMapper.Map).ToList();
    }

    private async Task<IReadOnlyList<DepartmentEmployeeOptionViewModel>> LoadEmployeeOptionsAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<DepartmentEmployeeOptionDto> employees =
            await assignmentService.GetAssignableEmployeesAsync(departmentId, cancellationToken);

        return employees.Select(SuperAdminViewModelMapper.Map).ToList();
    }

    private async Task<string> ResolveFacultyNameAsync(Guid facultyId, CancellationToken cancellationToken)
    {
        FacultyFormDto? faculty = await facultyAdminService.GetForEditAsync(facultyId, cancellationToken);
        return faculty?.Name ?? string.Empty;
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
            model.Name.Trim());
}
