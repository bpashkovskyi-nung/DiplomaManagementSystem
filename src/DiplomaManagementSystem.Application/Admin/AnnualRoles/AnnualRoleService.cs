using DiplomaManagementSystem.Application.Admin.AnnualRoles.Contracts;
using DiplomaManagementSystem.Application.Admin.AnnualRoles.Dtos;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.ReadModels;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Admin.AnnualRoles;

internal sealed class AnnualRoleService(
    IApplicationDbContext dbContext,
    IUserDisplayQueries userDisplayQueries,
    CurrentDepartmentResolver currentDepartmentResolver,
    IDepartmentAuthorizationService departmentAuthorization) : IAnnualRoleService
{
    private static readonly AnnualRoleType[] AllRoleTypes =
    [
        AnnualRoleType.DepartmentHead,
        AnnualRoleType.ExamCommissionSecretary,
        AnnualRoleType.AntiPlagiarismOfficer,
        AnnualRoleType.FormattingReviewer,
    ];

    public async Task<AnnualRolesPageDto?> GetPageAsync(Guid defenceSessionId, CancellationToken cancellationToken = default)
    {
        DefenceSession? session = await GetScopedSessionAsync(defenceSessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        List<AnnualRoleAssignment> assignments = await dbContext.AnnualRoleAssignments
            .AsNoTracking()
            .Where(assignment => assignment.DefenceSessionId == defenceSessionId)
            .ToListAsync(cancellationToken);

        var byRole = assignments.ToDictionary(a => a.RoleType);

        List<PersonOptionDto> employees = PersonOptionMapping.From(
            await userDisplayQueries.LoadEmployeeOptionsForDepartmentAsync(session.DepartmentId, cancellationToken));

        var assignedEmployeeIds = assignments
            .Select(assignment => assignment.EmployeeId)
            .ToHashSet();
        Dictionary<Guid, string> assignedNames = await userDisplayQueries.LoadFullNamesAsync(
            assignedEmployeeIds,
            cancellationToken);

        var slots = AllRoleTypes
            .Select(roleType =>
            {
                if (!byRole.TryGetValue(roleType, out AnnualRoleAssignment? assignment))
                {
                    return new AnnualRoleSlotDto(roleType, null, null);
                }

                string? name = employees.FirstOrDefault(e => e.Id == assignment.EmployeeId)?.FullName
                               ?? assignedNames.GetValueOrDefault(assignment.EmployeeId);
                return new AnnualRoleSlotDto(roleType, assignment.EmployeeId, name);
            })
            .ToList();

        string sessionLabel = SecretarySessionLabel.Format(session.Year, session.Type, session.Semester);

        return new AnnualRolesPageDto(session.Id, sessionLabel, slots, employees);
    }

    public async Task AssignAsync(AssignAnnualRoleDto request, CancellationToken cancellationToken = default)
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

        AnnualRoleAssignment? existing = await dbContext.AnnualRoleAssignments
            .FirstOrDefaultAsync(
                assignment => assignment.DefenceSessionId == request.DefenceSessionId
                              && assignment.RoleType == request.RoleType,
                cancellationToken);

        if (existing is not null)
        {
            existing.EmployeeId = request.EmployeeId;
            existing.AssignedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            dbContext.AnnualRoleAssignments.Add(new AnnualRoleAssignment
            {
                Id = Guid.NewGuid(),
                DefenceSessionId = request.DefenceSessionId,
                EmployeeId = request.EmployeeId,
                RoleType = request.RoleType,
                AssignedAt = DateTimeOffset.UtcNow,
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
