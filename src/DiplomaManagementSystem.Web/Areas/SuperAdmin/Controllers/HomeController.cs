using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;
using DiplomaManagementSystem.Web.Mapping;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin.Controllers;

public sealed class HomeController(
    Application.SuperAdmin.Faculties.Contracts.IFacultyAdminService facultyAdminService,
    IDepartmentAdminService departmentAdminService) : SuperAdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<FacultyListItemDto> faculties = await facultyAdminService.GetAllAsync(cancellationToken);
        IReadOnlyList<DepartmentListItemDto> departments =
            await departmentAdminService.GetAllAsync(cancellationToken: cancellationToken);

        SuperAdminHomeViewModel model = new()
        {
            Faculties = faculties.Select(SuperAdminViewModelMapper.MapOverview).ToList(),
            Departments = departments.Select(SuperAdminViewModelMapper.Map).ToList(),
        };

        return View(model);
    }
}
