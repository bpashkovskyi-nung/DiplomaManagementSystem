using System.Security.Claims;

using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Web.Areas.Admin.Models;
using DiplomaManagementSystem.Web.Departments;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Areas.Employee.Controllers;

[Area("Employee")]
[Authorize(Roles = RoleNames.Employee)]
public sealed class DepartmentController(IDepartmentContextService departmentContextService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Select(CancellationToken cancellationToken)
    {
        Guid userId = GetUserId();
        IReadOnlyList<DepartmentSelectOption> options =
            await departmentContextService.GetEmployeeSelectOptionsAsync(userId, cancellationToken);

        if (options.Count == 0)
        {
            return View(new DepartmentSelectViewModel());
        }

        if (options.Count == 1)
        {
            await departmentContextService.SetEmployeeDepartmentAsync(
                HttpContext,
                userId,
                options[0].Id,
                cancellationToken);
            return RedirectToAction("Index", "Home");
        }

        DepartmentSelectViewModel model = new()
        {
            Departments = options
                .Select(option => new SelectListItem(option.Label, option.Id.ToString()))
                .ToList(),
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Select(Guid departmentId, CancellationToken cancellationToken)
    {
        Guid userId = GetUserId();
        await departmentContextService.SetEmployeeDepartmentAsync(
            HttpContext,
            userId,
            departmentId,
            cancellationToken);

        return RedirectToAction("Index", "Home");
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
