using System.Security.Claims;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Web.Areas.Employee;
using DiplomaManagementSystem.Web.Areas.Employee.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Employee.Controllers;

[Area("Employee")]
[Authorize(Roles = RoleNames.Employee)]
public sealed class HomeController(IEmployeeHomeService employeeHomeService) : Controller
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

    private Guid GetUserId()
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing.");
        }

        return userId;
    }
}
