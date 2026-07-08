using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Web.Areas.Employee;
using DiplomaManagementSystem.Web.Areas.Employee.Models;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Employee.Controllers;

public sealed class HomeController(IEmployeeHomeService employeeHomeService) : EmployeeControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        EmployeeHomeDto home = await employeeHomeService.GetHomeAsync(GetUserId(), cancellationToken);

        EmployeeHomeViewModel model = new()
        {
            Sections = EmployeeRoleNavigationBuilder.BuildHomeSections(home.Roles),
        };

        return View(model);
    }
}
