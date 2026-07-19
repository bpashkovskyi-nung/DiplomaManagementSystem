using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Departments;

internal sealed class DefaultDepartmentResolver(IApplicationDbContext dbContext)
{
    public async Task<Guid> ResolveRequiredForNewSessionAsync(CancellationToken cancellationToken = default)
    {
        List<Guid> departmentIds = await dbContext.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .Select(department => department.Id)
            .ToListAsync(cancellationToken);

        if (departmentIds.Count == 1)
        {
            return departmentIds[0];
        }

        throw new DomainException("Department context is required to create a defence session.");
    }
}
