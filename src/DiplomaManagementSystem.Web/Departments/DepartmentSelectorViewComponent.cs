using System.Security.Claims;

using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Departments.Contracts;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Departments;

public sealed class DepartmentSelectorViewComponent(
    IDepartmentContextService departmentContextService,
    IDepartmentContext departmentContext) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(string areaName)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        string? userIdValue = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            return Content(string.Empty);
        }

        IReadOnlyList<DepartmentSelectOption> options = areaName switch
        {
            "Admin" when departmentContextService.CanAccessAdminArea(UserClaimsPrincipal) =>
                await departmentContextService.GetAdminSelectOptionsAsync(userId, HttpContext.RequestAborted),
            "Employee" when UserClaimsPrincipal.IsInRole(RoleNames.Employee) =>
                await departmentContextService.GetEmployeeSelectOptionsAsync(userId, HttpContext.RequestAborted),
            _ => [],
        };

        if (options.Count <= 1)
        {
            return Content(string.Empty);
        }

        Guid? selectedDepartmentId = departmentContext.CurrentDepartmentId;
        DepartmentSelectorViewModel model = new()
        {
            AreaName = areaName,
            SelectedDepartmentId = selectedDepartmentId,
            Departments = options
                .Select(option => new SelectListItem(
                    option.Label,
                    option.Id.ToString(),
                    option.Id == selectedDepartmentId))
                .ToList(),
        };

        return View(model);
    }
}
