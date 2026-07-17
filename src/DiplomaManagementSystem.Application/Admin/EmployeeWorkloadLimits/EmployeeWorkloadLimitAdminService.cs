using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Contracts;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits;

internal sealed class EmployeeWorkloadLimitAdminService(
    IApplicationDbContext dbContext,
    IEmployeeWorkloadLimitQueries workloadLimitQueries,
    IUserDisplayQueries userDisplayQueries,
    CurrentDepartmentResolver currentDepartmentResolver,
    IDepartmentAuthorizationService departmentAuthorization) : IEmployeeWorkloadLimitAdminService
{
    public async Task<EmployeeWorkloadLimitsPageDto?> GetPageAsync(
        Guid defenceSessionId,
        CancellationToken cancellationToken = default)
    {
        DefenceSession? session = await GetScopedSessionAsync(defenceSessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        List<UserOption> employees = await userDisplayQueries.LoadEmployeeOptionsForDepartmentAsync(
            session.DepartmentId,
            cancellationToken);

        Dictionary<Guid, EmployeeSessionWorkloadLimit> limits = await dbContext.EmployeeSessionWorkloadLimits
            .AsNoTracking()
            .Where(limit => limit.DefenceSessionId == defenceSessionId)
            .ToDictionaryAsync(limit => limit.EmployeeId, cancellationToken);

        List<EmployeeWorkloadLimitRowDto> rows = [];
        foreach (UserOption employee in employees)
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
                employee.Email,
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
        DefenceSession session = await GetScopedSessionAsync(request.DefenceSessionId, cancellationToken)
                                 ?? throw new DomainException($"Defence session {request.DefenceSessionId} not found.");

        bool employeeExists = await userDisplayQueries.IsActiveDepartmentEmployeeAsync(
            request.EmployeeId,
            session.DepartmentId,
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

    private async Task<DefenceSession?> GetScopedSessionAsync(Guid defenceSessionId, CancellationToken cancellationToken)
    {
        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == defenceSessionId, cancellationToken);

        if (session is null)
        {
            return null;
        }

        Guid departmentId = await currentDepartmentResolver.ResolveRequiredScopedDepartmentIdAsync(cancellationToken);
        await departmentAuthorization.EnsureSessionInDepartmentAsync(defenceSessionId, departmentId, cancellationToken);
        return session;
    }
}
