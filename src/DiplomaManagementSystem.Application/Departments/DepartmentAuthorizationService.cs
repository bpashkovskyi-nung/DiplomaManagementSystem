using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Departments;

internal sealed class DepartmentAuthorizationService(
    IApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IDepartmentAuthorizationService
{
    public async Task<bool> IsSuperAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userManager.FindByIdAsync(userId.ToString());
        return user is not null && await userManager.IsInRoleAsync(user, RoleNames.SuperAdmin);
    }

    public async Task<IReadOnlyList<Guid>> GetAdminDepartmentIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DepartmentAdminAssignments
            .AsNoTracking()
            .Where(assignment => assignment.UserId == userId)
            .Select(assignment => assignment.DepartmentId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetEmployeeDepartmentIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.DepartmentEmployees
            .AsNoTracking()
            .Where(employee => employee.UserId == userId && employee.IsActive)
            .Select(employee => employee.DepartmentId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task EnsureDepartmentAdminAccessAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (await IsSuperAdminAsync(userId, cancellationToken))
        {
            return;
        }

        bool hasAccess = await dbContext.DepartmentAdminAssignments
            .AsNoTracking()
            .AnyAsync(
                assignment => assignment.UserId == userId && assignment.DepartmentId == departmentId,
                cancellationToken);

        if (!hasAccess)
        {
            throw new DomainException(DepartmentMessages.AccessDenied);
        }
    }

    public async Task EnsureDepartmentEmployeeAccessAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        bool hasAccess = await dbContext.DepartmentEmployees
            .AsNoTracking()
            .AnyAsync(
                employee => employee.UserId == userId
                            && employee.DepartmentId == departmentId
                            && employee.IsActive,
                cancellationToken);

        if (!hasAccess)
        {
            throw new DomainException(DepartmentMessages.AccessDenied);
        }
    }

    public async Task EnsureSessionInDepartmentAsync(
        Guid sessionId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        bool belongs = await dbContext.DefenceSessions
            .AsNoTracking()
            .AnyAsync(
                session => session.Id == sessionId && session.DepartmentId == departmentId,
                cancellationToken);

        if (!belongs)
        {
            throw new DomainException(DepartmentMessages.SessionNotInDepartment);
        }
    }
}
