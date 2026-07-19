using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Queries;

internal sealed class EmployeeWorkloadLimitQueries(ApplicationDbContext dbContext) : IEmployeeWorkloadLimitQueries
{
    public Task<int?> GetSupervisorLimitAsync(
        Guid defenceSessionId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.EmployeeSessionWorkloadLimits
            .AsNoTracking()
            .Where(limit => limit.DefenceSessionId == defenceSessionId && limit.EmployeeId == employeeId)
            .Select(limit => limit.MaxSupervisorStudents)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int?> GetReviewerLimitAsync(
        Guid defenceSessionId,
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.EmployeeSessionWorkloadLimits
            .AsNoTracking()
            .Where(limit => limit.DefenceSessionId == defenceSessionId && limit.EmployeeId == employeeId)
            .Select(limit => limit.MaxReviewerStudents)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountConfirmedSupervisorStudentsAsync(
        Guid defenceSessionId,
        Guid employeeId,
        Guid? excludeDiplomaId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Domain.Entities.Diploma> query = dbContext.Diplomas
            .AsNoTracking()
            .Where(diploma => diploma.DefenceSessionId == defenceSessionId
                              && diploma.SupervisorId == employeeId
                              && diploma.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Confirmed);

        if (excludeDiplomaId.HasValue)
        {
            query = query.Where(diploma => diploma.Id != excludeDiplomaId.Value);
        }

        return query.CountAsync(cancellationToken);
    }

    public Task<int> CountReviewerAssignmentsAsync(
        Guid defenceSessionId,
        Guid employeeId,
        Guid? excludeDiplomaId = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Domain.Entities.Diploma> query = dbContext.Diplomas
            .AsNoTracking()
            .Where(diploma => diploma.DefenceSessionId == defenceSessionId
                              && diploma.ReviewerId == employeeId);

        if (excludeDiplomaId.HasValue)
        {
            query = query.Where(diploma => diploma.Id != excludeDiplomaId.Value);
        }

        return query.CountAsync(cancellationToken);
    }
}
