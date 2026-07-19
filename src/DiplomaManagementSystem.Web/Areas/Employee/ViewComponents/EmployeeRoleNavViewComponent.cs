using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Web.Areas.Employee.Models;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Employee.ViewComponents;

public sealed class EmployeeRoleNavViewComponent(IEmployeeHomeService employeeHomeService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(EmployeeRoleArea activeRole)
    {
        Guid userId = GetUserId();
        EmployeeHomeDto home = await employeeHomeService.GetHomeAsync(userId, HttpContext.RequestAborted);

        string currentController = ViewContext.RouteData.Values["controller"]?.ToString() ?? string.Empty;
        string currentAction = ViewContext.RouteData.Values["action"]?.ToString() ?? string.Empty;

        EmployeeRoleNavViewModel model = EmployeeRoleNavigationBuilder.Build(
            home,
            activeRole,
            currentController,
            currentAction);

        return View(model);
    }

    private Guid GetUserId()
    {
        string? userIdValue = HttpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing.");
        }

        return userId;
    }
}
