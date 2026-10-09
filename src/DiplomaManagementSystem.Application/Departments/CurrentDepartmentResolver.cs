using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Departments;

internal sealed class CurrentDepartmentResolver(
    IDepartmentContext departmentContext,
    IDepartmentAuthorizationService departmentAuthorization,
    IApplicationDbContext dbContext)
{
    public async Task<Guid> ResolveRequiredAdminDepartmentIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (departmentContext.CurrentDepartmentId is Guid fromContext)
        {
            await departmentAuthorization.EnsureDepartmentAdminAccessAsync(userId, fromContext, cancellationToken);
            return fromContext;
        }

        IReadOnlyList<Guid> assignments =
            await departmentAuthorization.GetAdminDepartmentIdsAsync(userId, cancellationToken);

        if (assignments.Count == 1)
        {
            return assignments[0];
        }

        if (assignments.Count == 0 && await departmentAuthorization.IsSuperAdminAsync(userId, cancellationToken))
        {
            Guid? onlyDepartmentId = await TryGetSingleDepartmentIdAsync(cancellationToken);
            if (onlyDepartmentId is Guid departmentId)
            {
                return departmentId;
            }
        }

        throw new DomainException(DepartmentMessages.ContextRequired);
    }

    public async Task<Guid> ResolveRequiredEmployeeDepartmentIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (departmentContext.CurrentDepartmentId is Guid fromContext)
        {
            await departmentAuthorization.EnsureDepartmentEmployeeAccessAsync(userId, fromContext, cancellationToken);
            return fromContext;
        }

        IReadOnlyList<Guid> memberships =
            await departmentAuthorization.GetEmployeeDepartmentIdsAsync(userId, cancellationToken);

        if (memberships.Count == 1)
        {
            return memberships[0];
        }

        throw new DomainException(DepartmentMessages.ContextRequired);
    }

    public async Task<Guid?> TryResolveScopedDepartmentIdAsync(CancellationToken cancellationToken = default)
    {
        if (departmentContext.CurrentDepartmentId is Guid fromContext)
        {
            return fromContext;
        }

        return await TryGetSingleDepartmentIdAsync(cancellationToken);
    }

    public async Task<Guid> ResolveRequiredScopedDepartmentIdAsync(CancellationToken cancellationToken = default)
    {
        Guid? departmentId = await TryResolveScopedDepartmentIdAsync(cancellationToken);
        if (departmentId is Guid id)
        {
            return id;
        }

        throw new DomainException(DepartmentMessages.ContextRequired);
    }

    private async Task<Guid?> TryGetSingleDepartmentIdAsync(CancellationToken cancellationToken)
    {
        List<Guid> departmentIds = await dbContext.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .Select(department => department.Id)
            .ToListAsync(cancellationToken);

        return departmentIds.Count == 1 ? departmentIds[0] : null;
    }
}
