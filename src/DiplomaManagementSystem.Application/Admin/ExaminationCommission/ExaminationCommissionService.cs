using DiplomaManagementSystem.Application.Admin.ExaminationCommission.Contracts;
using DiplomaManagementSystem.Application.Admin.ExaminationCommission.Dtos;
using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Admin.ExaminationCommission;

internal sealed class ExaminationCommissionService(
    IApplicationDbContext dbContext,
    CurrentDepartmentResolver currentDepartmentResolver,
    IDepartmentAuthorizationService departmentAuthorization) : IExaminationCommissionService
{
    public async Task<ExaminationCommissionEditorDto?> GetEditorAsync(
        Guid defenceSessionId,
        CancellationToken cancellationToken = default)
    {
        DefenceSession? session = await GetScopedSessionAsync(defenceSessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        List<ExaminationCommissionParticipant> participants = await dbContext.ExaminationCommissionParticipants
            .AsNoTracking()
            .Where(participant => participant.DefenceSessionId == defenceSessionId)
            .OrderBy(participant => participant.Role)
            .ThenBy(participant => participant.SortOrder)
            .ThenBy(participant => participant.CreatedAt)
            .ToListAsync(cancellationToken);

        ExaminationCommissionParticipant? chairEntity = participants
            .FirstOrDefault(participant => participant.Role == ExaminationCommissionRole.Chair);
        List<ExaminationCommissionParticipantDto> members = participants
            .Where(participant => participant.Role == ExaminationCommissionRole.Member)
            .Select(ToDto)
            .ToList();

        ExaminationCommissionDto commission = new(
            chairEntity is null ? null : ToDto(chairEntity),
            members);

        List<CommissionEmployeeOptionDto> employees = await LoadEmployeeOptionsAsync(
            session.DepartmentId,
            cancellationToken);

        return new ExaminationCommissionEditorDto(
            session.Id,
            SecretarySessionLabel.Format(session.Year, session.Type, session.Semester),
            commission,
            employees);
    }

    public async Task SaveAsync(SaveExaminationCommissionDto request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Chair);
        ArgumentNullException.ThrowIfNull(request.Members);

        DefenceSession session = await GetScopedSessionAsync(request.DefenceSessionId, cancellationToken)
                                 ?? throw new DomainException($"Defence session {request.DefenceSessionId} not found.");

        if (request.Members.Count < SaveExaminationCommissionValidator.MinimumMemberCount)
        {
            throw new DomainException(
                $"Склад ЕК повинен містити щонайменше {SaveExaminationCommissionValidator.MinimumMemberCount} членів.");
        }

        Dictionary<Guid, DepartmentEmployee> employeesByUserId = await dbContext.DepartmentEmployees
            .AsNoTracking()
            .Where(employee => employee.DepartmentId == session.DepartmentId && employee.IsActive)
            .ToDictionaryAsync(employee => employee.UserId, cancellationToken);

        List<(ExaminationCommissionRole Role, string FullName, string Position, Guid? EmployeeId, int SortOrder)>
            resolved =
            [
                ResolveParticipant(
                    request.Chair,
                    ExaminationCommissionRole.Chair,
                    sortOrder: 0,
                    employeesByUserId),
            ];

        for (int index = 0; index < request.Members.Count; index++)
        {
            resolved.Add(
                ResolveParticipant(
                    request.Members[index],
                    ExaminationCommissionRole.Member,
                    sortOrder: index + 1,
                    employeesByUserId));
        }

        EnsureNoDuplicateEmployees(resolved);

        List<ExaminationCommissionParticipant> existing = await dbContext.ExaminationCommissionParticipants
            .Where(participant => participant.DefenceSessionId == request.DefenceSessionId)
            .ToListAsync(cancellationToken);
        dbContext.ExaminationCommissionParticipants.RemoveRange(existing);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach ((ExaminationCommissionRole role, string fullName, string position, Guid? employeeId, int sortOrder)
                 in resolved)
        {
            dbContext.ExaminationCommissionParticipants.Add(new ExaminationCommissionParticipant
            {
                Id = Guid.NewGuid(),
                DefenceSessionId = request.DefenceSessionId,
                Role = role,
                EmployeeId = employeeId,
                FullName = fullName,
                Position = position,
                SortOrder = sortOrder,
                CreatedAt = now,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ExaminationCommissionParticipantDto ToDto(ExaminationCommissionParticipant participant) =>
        new(
            participant.Role,
            participant.EmployeeId,
            participant.FullName,
            participant.Position,
            participant.SortOrder);

    private async Task<List<CommissionEmployeeOptionDto>> LoadEmployeeOptionsAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var rows = await dbContext.DepartmentEmployees
            .AsNoTracking()
            .Where(employee => employee.DepartmentId == departmentId && employee.IsActive)
            .Join(
                dbContext.Users.AsNoTracking().Where(user => user.UserKind == UserKind.Employee),
                employee => employee.UserId,
                user => user.Id,
                (employee, user) => new
                {
                    user.Id,
                    employee.FullName,
                    Email = user.Email ?? string.Empty,
                    employee.AcademicRank,
                })
            .OrderBy(pair => pair.FullName)
            .ToListAsync(cancellationToken);

        return rows
            .Select(pair => new CommissionEmployeeOptionDto(
                pair.Id,
                pair.FullName,
                pair.Email,
                pair.AcademicRank is EmployeeAcademicRank rank
                    ? AcademicRankLabels.GetDisplayName(rank)
                    : null))
            .ToList();
    }

    private static (ExaminationCommissionRole Role, string FullName, string Position, Guid? EmployeeId, int SortOrder)
        ResolveParticipant(
            SaveExaminationCommissionParticipantDto input,
            ExaminationCommissionRole role,
            int sortOrder,
            IReadOnlyDictionary<Guid, DepartmentEmployee> employeesByUserId)
    {
        if (input.IsExternal)
        {
            string fullName = (input.FullName ?? string.Empty).Trim();
            string position = (input.Position ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(position))
            {
                throw new DomainException("Для зовнішньої особи потрібно вказати ПІБ і посаду.");
            }

            return (role, fullName, position, null, sortOrder);
        }

        if (input.EmployeeId is not Guid employeeId)
        {
            throw new DomainException("Оберіть викладача кафедри.");
        }

        if (!employeesByUserId.TryGetValue(employeeId, out DepartmentEmployee? employee))
        {
            throw new DomainException($"Employee {employeeId} not found.");
        }

        if (employee.AcademicRank is not EmployeeAcademicRank rank)
        {
            throw new DomainException(
                $"Для викладача «{employee.FullName}» потрібно заповнити вчене звання.");
        }

        return (
            role,
            employee.FullName.Trim(),
            AcademicRankLabels.GetDisplayName(rank),
            employeeId,
            sortOrder);
    }

    private static void EnsureNoDuplicateEmployees(
        IReadOnlyList<(ExaminationCommissionRole Role, string FullName, string Position, Guid? EmployeeId, int SortOrder)>
            participants)
    {
        HashSet<Guid> seen = [];
        foreach ((_, _, _, Guid? employeeId, _) in participants)
        {
            if (employeeId is not Guid id)
            {
                continue;
            }

            if (!seen.Add(id))
            {
                throw new DomainException("Один викладач не може бути призначений у складі ЕК двічі.");
            }
        }
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
