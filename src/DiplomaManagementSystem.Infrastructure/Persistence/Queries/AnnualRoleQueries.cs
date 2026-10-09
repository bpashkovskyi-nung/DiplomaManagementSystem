using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Queries;

internal sealed class AnnualRoleQueries(ApplicationDbContext dbContext) : IAnnualRoleQueries
{
    public Task<List<Guid>> GetSessionIdsAsync(
        Guid employeeId,
        AnnualRoleType roleType,
        CancellationToken cancellationToken = default)
    {
        return dbContext.AnnualRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.EmployeeId == employeeId && assignment.RoleType == roleType)
            .Select(assignment => assignment.DefenceSessionId)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasRoleForSessionAsync(
        Guid employeeId,
        Guid defenceSessionId,
        AnnualRoleType roleType,
        CancellationToken cancellationToken = default)
    {
        return dbContext.AnnualRoleAssignments.AnyAsync(
            assignment => assignment.EmployeeId == employeeId
                          && assignment.DefenceSessionId == defenceSessionId
                          && assignment.RoleType == roleType,
            cancellationToken);
    }

    public Task<bool> IsSecretaryAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.AnnualRoleAssignments.AnyAsync(
            assignment => assignment.EmployeeId == userId
                          && assignment.RoleType == AnnualRoleType.ExamCommissionSecretary,
            cancellationToken);
    }

    public Task<bool> CanAccessSessionAsSecretaryAsync(
        Guid userId,
        Guid defenceSessionId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.AnnualRoleAssignments.AnyAsync(
            assignment => assignment.DefenceSessionId == defenceSessionId
                          && assignment.EmployeeId == userId
                          && assignment.RoleType == AnnualRoleType.ExamCommissionSecretary,
            cancellationToken);
    }

    public Task<List<SecretarySessionRow>> ListAccessibleSecretarySessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.AnnualRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.EmployeeId == userId
                                 && assignment.RoleType == AnnualRoleType.ExamCommissionSecretary)
            .Join(
                dbContext.DefenceSessions.AsNoTracking(),
                assignment => assignment.DefenceSessionId,
                session => session.Id,
                (assignment, session) => new { session })
            .OrderByDescending(row => row.session.Year)
            .ThenBy(row => row.session.Type)
            .ThenBy(row => row.session.Semester)
            .Select(row => new SecretarySessionRow(
                row.session.Id,
                row.session.Year,
                row.session.Type,
                row.session.Semester))
            .ToListAsync(cancellationToken);
    }

    public Task<Guid?> GetEmployeeIdForSessionRoleAsync(
        Guid defenceSessionId,
        AnnualRoleType roleType,
        CancellationToken cancellationToken = default)
    {
        return dbContext.AnnualRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.DefenceSessionId == defenceSessionId
                                 && assignment.RoleType == roleType)
            .Select(assignment => (Guid?)assignment.EmployeeId)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
