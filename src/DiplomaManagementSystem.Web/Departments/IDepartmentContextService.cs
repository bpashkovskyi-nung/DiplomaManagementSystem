using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Departments;

public interface IDepartmentContextService
{
    bool CanAccessAdminArea(ClaimsPrincipal user);

    Task<IReadOnlyList<DepartmentSelectOption>> GetAdminSelectOptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentSelectOption>> GetEmployeeSelectOptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IActionResult?> EnsureAdminContextAsync(
        HttpContext httpContext,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IActionResult?> EnsureEmployeeContextAsync(
        HttpContext httpContext,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task SetAdminDepartmentAsync(
        HttpContext httpContext,
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task SetEmployeeDepartmentAsync(
        HttpContext httpContext,
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default);
}
