using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Contracts;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits;

internal sealed class EmployeeWorkloadLimitAdminService(
    IApplicationDbContext dbContext,
    IEmployeeWorkloadLimitQueries workloadLimitQueries) : IEmployeeWorkloadLimitAdminService
{
    public async Task<EmployeeWorkloadLimitsPageDto?> GetPageAsync(
        Guid defenceSessionId,
        CancellationToken cancellationToken = default)
    {
        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == defenceSessionId, cancellationToken);

        if (session is null)
        {
            return null;
        }

        List<ApplicationUser> employees = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserKind == UserKind.Employee)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, EmployeeSessionWorkloadLimit> limits = await dbContext.EmployeeSessionWorkloadLimits
            .AsNoTracking()
            .Where(limit => limit.DefenceSessionId == defenceSessionId)
            .ToDictionaryAsync(limit => limit.EmployeeId, cancellationToken);

        List<EmployeeWorkloadLimitRowDto> rows = [];
        foreach (ApplicationUser employee in employees)
        {
            limits.TryGetValue(employee.Id, out EmployeeSessionWorkloadLimit? limit);

            int supervisorCount = await workloadLimitQueries.CountConfirmedSupervisorStudentsAsync(
                defenceSessionId,
                employee.Id,
                cancellationToken: cancellationToken);

            int reviewerCount = await workloadLimitQueries.CountReviewerAssignmentsAsync(
                defenceSessionId,
                employee.Id,
                cancellationToken: cancellationToken);

            rows.Add(new EmployeeWorkloadLimitRowDto(
                employee.Id,
                employee.FullName,
                employee.Email ?? string.Empty,
                limit?.MaxSupervisorStudents,
                limit?.MaxReviewerStudents,
                supervisorCount,
                reviewerCount));
        }

        string sessionLabel = SecretarySessionLabel.Format(session.Year, session.Type, session.Semester);

        return new EmployeeWorkloadLimitsPageDto(defenceSessionId, sessionLabel, rows);
    }

    public async Task SetLimitAsync(SetEmployeeWorkloadLimitDto request, CancellationToken cancellationToken = default)
    {
        bool sessionExists = await dbContext.DefenceSessions.AnyAsync(
            session => session.Id == request.DefenceSessionId,
            cancellationToken);

        if (!sessionExists)
        {
            throw new DomainException($"Defence session {request.DefenceSessionId} not found.");
        }

        bool employeeExists = await dbContext.Users.AnyAsync(
            user => user.Id == request.EmployeeId && user.UserKind == UserKind.Employee,
            cancellationToken);

        if (!employeeExists)
        {
            throw new DomainException($"Employee {request.EmployeeId} not found.");
        }

        if (!request.MaxSupervisorStudents.HasValue && !request.MaxReviewerStudents.HasValue)
        {
            EmployeeSessionWorkloadLimit? existingToRemove = await dbContext.EmployeeSessionWorkloadLimits
                .FirstOrDefaultAsync(
                    limit => limit.DefenceSessionId == request.DefenceSessionId
                             && limit.EmployeeId == request.EmployeeId,
                    cancellationToken);

            if (existingToRemove is not null)
            {
                dbContext.EmployeeSessionWorkloadLimits.Remove(existingToRemove);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        EmployeeSessionWorkloadLimit? existing = await dbContext.EmployeeSessionWorkloadLimits
            .FirstOrDefaultAsync(
                limit => limit.DefenceSessionId == request.DefenceSessionId
                         && limit.EmployeeId == request.EmployeeId,
                cancellationToken);

        if (existing is not null)
        {
            existing.MaxSupervisorStudents = request.MaxSupervisorStudents;
            existing.MaxReviewerStudents = request.MaxReviewerStudents;
            existing.UpdatedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            dbContext.EmployeeSessionWorkloadLimits.Add(new EmployeeSessionWorkloadLimit
            {
                Id = Guid.NewGuid(),
                DefenceSessionId = request.DefenceSessionId,
                EmployeeId = request.EmployeeId,
                MaxSupervisorStudents = request.MaxSupervisorStudents,
                MaxReviewerStudents = request.MaxReviewerStudents,
                UpdatedAt = DateTimeOffset.UtcNow,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
