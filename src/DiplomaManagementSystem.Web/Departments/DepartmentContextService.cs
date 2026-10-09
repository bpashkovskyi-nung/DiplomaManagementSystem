using System.Security.Claims;

using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Web.Departments;

internal sealed class DepartmentContextService(
    IDepartmentContext departmentContext,
    IDepartmentSessionService departmentSessionService,
    IDepartmentAuthorizationService departmentAuthorization,
    IApplicationDbContext dbContext) : IDepartmentContextService
{
    public bool CanAccessAdminArea(ClaimsPrincipal user) =>
        user.IsInRole(RoleNames.Admin)
        || (user.IsInRole(RoleNames.SuperAdmin) && departmentContext.IsSuperAdminImpersonating);

    public async Task<IReadOnlyList<DepartmentSelectOption>> GetAdminSelectOptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> departmentIds =
            await departmentAuthorization.GetAdminDepartmentIdsAsync(userId, cancellationToken);

        if (departmentIds.Count == 0
            && departmentContext.IsSuperAdminImpersonating
            && departmentContext.CurrentDepartmentId is Guid impersonatedDepartmentId
            && await departmentAuthorization.IsSuperAdminAsync(userId, cancellationToken))
        {
            return await LoadOptionsAsync([impersonatedDepartmentId], cancellationToken);
        }

        return await LoadOptionsAsync(departmentIds, cancellationToken);
    }

    public async Task<IReadOnlyList<DepartmentSelectOption>> GetEmployeeSelectOptionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> departmentIds =
            await departmentAuthorization.GetEmployeeDepartmentIdsAsync(userId, cancellationToken);

        return await LoadOptionsAsync(departmentIds, cancellationToken);
    }

    public async Task<IActionResult?> EnsureAdminContextAsync(
        HttpContext httpContext,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!CanAccessAdminArea(httpContext.User))
        {
            return new ForbidResult();
        }

        if (departmentContext.CurrentDepartmentId is Guid currentDepartmentId)
        {
            try
            {
                await EnsureAdminDepartmentAccessAsync(userId, currentDepartmentId, cancellationToken);
                return null;
            }
            catch (DomainException)
            {
                departmentSessionService.ClearSelectedDepartment(httpContext);
            }
        }

        IReadOnlyList<DepartmentSelectOption> options =
            await GetAdminSelectOptionsAsync(userId, cancellationToken);

        if (options.Count == 0)
        {
            return new ForbidResult();
        }

        if (options.Count == 1)
        {
            IReadOnlyList<Guid> assignments =
                await departmentAuthorization.GetAdminDepartmentIdsAsync(userId, cancellationToken);
            bool impersonating = await departmentAuthorization.IsSuperAdminAsync(userId, cancellationToken)
                                 && !assignments.Contains(options[0].Id);
            departmentSessionService.SetSelectedDepartment(
                httpContext,
                options[0].Id,
                superAdminImpersonating: impersonating);
            return null;
        }

        return new RedirectToActionResult(
            "Select",
            "Department",
            new { area = "Admin" });
    }

    public async Task<IActionResult?> EnsureEmployeeContextAsync(
        HttpContext httpContext,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (departmentContext.CurrentDepartmentId is Guid currentDepartmentId)
        {
            try
            {
                await departmentAuthorization.EnsureDepartmentEmployeeAccessAsync(
                    userId,
                    currentDepartmentId,
                    cancellationToken);
                return null;
            }
            catch (DomainException)
            {
                departmentSessionService.ClearSelectedDepartment(httpContext);
            }
        }

        IReadOnlyList<DepartmentSelectOption> options =
            await GetEmployeeSelectOptionsAsync(userId, cancellationToken);

        if (options.Count == 0)
        {
            return new ForbidResult();
        }

        if (options.Count == 1)
        {
            departmentSessionService.SetSelectedDepartment(httpContext, options[0].Id);
            return null;
        }

        return new RedirectToActionResult(
            "Select",
            "Department",
            new { area = "Employee" });
    }

    public async Task SetAdminDepartmentAsync(
        HttpContext httpContext,
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAdminDepartmentAccessAsync(userId, departmentId, cancellationToken);

        IReadOnlyList<Guid> assignments =
            await departmentAuthorization.GetAdminDepartmentIdsAsync(userId, cancellationToken);
        bool impersonating = await departmentAuthorization.IsSuperAdminAsync(userId, cancellationToken)
                             && !assignments.Contains(departmentId);

        departmentSessionService.SetSelectedDepartment(
            httpContext,
            departmentId,
            superAdminImpersonating: impersonating);
    }

    public async Task SetEmployeeDepartmentAsync(
        HttpContext httpContext,
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        await departmentAuthorization.EnsureDepartmentEmployeeAccessAsync(userId, departmentId, cancellationToken);
        departmentSessionService.SetSelectedDepartment(httpContext, departmentId);
    }

    private async Task EnsureAdminDepartmentAccessAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Guid> assignments =
            await departmentAuthorization.GetAdminDepartmentIdsAsync(userId, cancellationToken);

        if (assignments.Contains(departmentId))
        {
            return;
        }

        if (departmentContext.IsSuperAdminImpersonating
            && await departmentAuthorization.IsSuperAdminAsync(userId, cancellationToken))
        {
            return;
        }

        await departmentAuthorization.EnsureDepartmentAdminAccessAsync(userId, departmentId, cancellationToken);
    }

    private async Task<IReadOnlyList<DepartmentSelectOption>> LoadOptionsAsync(
        IReadOnlyList<Guid> departmentIds,
        CancellationToken cancellationToken)
    {
        if (departmentIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Departments
            .AsNoTracking()
            .Where(department => departmentIds.Contains(department.Id) && department.IsActive)
            .Join(
                dbContext.Faculties.AsNoTracking(),
                department => department.FacultyId,
                faculty => faculty.Id,
                (department, faculty) => new { department, faculty })
            .OrderBy(item => item.faculty.Name)
            .ThenBy(item => item.department.Name)
            .Select(item => new DepartmentSelectOption(
                item.department.Id,
                item.faculty.Name + " — " + item.department.Name))
            .ToListAsync(cancellationToken);
    }
}
