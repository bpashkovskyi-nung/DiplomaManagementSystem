using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin.Controllers;

public sealed class HomeController(
    Application.SuperAdmin.Faculties.Contracts.IFacultyAdminService facultyAdminService) : SuperAdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<FacultyListItemDto> faculties = await facultyAdminService.GetAllAsync(cancellationToken);

        SuperAdminHomeViewModel model = new()
        {
            FacultyCount = faculties.Count,
        };

        return View(model);
    }
}
