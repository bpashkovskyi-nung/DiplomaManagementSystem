using DiplomaManagementSystem.Application.Admin.Students.Contracts;
using DiplomaManagementSystem.Application.Admin.Students.Dtos;
using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Admin.Students;

internal sealed class StudentAdminService(
    IApplicationDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    IUserProvisioningService userProvisioningService,
    DiplomaCreationService diplomaCreationService,
    CurrentDepartmentResolver currentDepartmentResolver) : IStudentAdminService
{
    public async Task<IReadOnlyList<StudentListItemDto>> GetAllAsync(
        Guid? defenceSessionId = null,
        CancellationToken cancellationToken = default)
    {
        Guid departmentId = await currentDepartmentResolver.ResolveRequiredScopedDepartmentIdAsync(cancellationToken);

        IQueryable<ApplicationUser> query = dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserKind == UserKind.Student
                           && user.DefenceSessionId != null
                           && dbContext.DefenceSessions.Any(session =>
                               session.Id == user.DefenceSessionId
                               && session.DepartmentId == departmentId));

        if (defenceSessionId.HasValue)
        {
            query = query.Where(user => user.DefenceSessionId == defenceSessionId.Value);
        }

        List<ApplicationUser> students = await query
            .ToListAsync(cancellationToken);

        students = students
            .OrderBy(student => PersonNameSort.SurnameKey(student.FullName), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(student => student.FullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        Dictionary<Guid, string> groupNames = await dbContext.StudyGroups
            .AsNoTracking()
            .ToDictionaryAsync(group => group.Id, group => group.Name, cancellationToken);

        Dictionary<Guid, DefenceSession> sessions = await dbContext.DefenceSessions
            .AsNoTracking()
            .ToDictionaryAsync(session => session.Id, cancellationToken);

        return students
            .Select(student => MapListItem(student, groupNames, sessions))
            .ToList();
    }

    public async Task<StudentFormDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplicationUser? student = await FindStudentAsync(id, asNoTracking: true, cancellationToken);
        if (student is null
            || student.StudyGroupId is not Guid studyGroupId
            || student.DefenceSessionId is not Guid defenceSessionId)
        {
            return null;
        }

        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(defenceSession => defenceSession.Id == defenceSessionId, cancellationToken);

        string? sessionLabel = session is null
            ? null
            : SecretarySessionLabel.Format(session.Year, session.Type, session.Semester);

        return new StudentFormDto(
            student.Id,
            student.FullName,
            student.Email ?? string.Empty,
            defenceSessionId,
            studyGroupId,
            session?.Type,
            sessionLabel);
    }

    public async Task<StudentDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplicationUser? student = await FindStudentAsync(id, asNoTracking: true, cancellationToken);
        if (student is null
            || student.StudyGroupId is not Guid studyGroupId
            || student.DefenceSessionId is not Guid defenceSessionId)
        {
            return null;
        }

        StudyGroup? group = await dbContext.StudyGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(studyGroup => studyGroup.Id == studyGroupId, cancellationToken);

        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(defenceSession => defenceSession.Id == defenceSessionId, cancellationToken);

        bool hasDiploma = await dbContext.Diplomas
            .AnyAsync(diploma => diploma.StudentId == id, cancellationToken);

        return new StudentDetailsDto(
            student.Id,
            student.FullName,
            student.Email ?? string.Empty,
            defenceSessionId,
            session!.Type,
            FormatSessionLabel(session),
            studyGroupId,
            group?.Name ?? "—",
            hasDiploma,
            student.CreatedAt);
    }

    public async Task<IReadOnlyList<StudentSessionOptionDto>> GetSessionOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        Guid departmentId = await currentDepartmentResolver.ResolveRequiredScopedDepartmentIdAsync(cancellationToken);

        return await dbContext.DefenceSessions
            .AsNoTracking()
            .Where(session => session.Status == DefenceSessionStatus.Active
                              && session.DepartmentId == departmentId)
            .OrderByDescending(session => session.Year)
            .ThenBy(session => session.Type)
            .Select(session => new StudentSessionOptionDto(
                session.Id,
                SecretarySessionLabel.Format(session.Year, session.Type, session.Semester),
                session.Type))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudentGroupOptionDto>> GetGroupOptionsAsync(
        Guid defenceSessionId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.StudyGroups
            .AsNoTracking()
            .Where(group => group.DefenceSessionId == defenceSessionId)
            .OrderBy(group => group.Name)
            .Select(group => new StudentGroupOptionDto(group.Id, group.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> CreateAsync(StudentFormDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationUser user = await userProvisioningService.CreateStudentAsync(
            dto.FullName.Trim(),
            dto.Email.Trim(),
            dto.DefenceSessionId,
            dto.StudyGroupId,
            cancellationToken);

        await EnsureDiplomaAsync(user.Id, dto.DefenceSessionId, cancellationToken);

        return user.Id;
    }

    public async Task UpdateAsync(Guid id, StudentFormDto dto, CancellationToken cancellationToken = default)
    {
        ApplicationUser? student = await FindStudentAsync(id, asNoTracking: false, cancellationToken);
        if (student is null)
        {
            throw new DomainException("Student not found.");
        }

        await EnsureStudentSessionAndGroupAsync(dto.DefenceSessionId, dto.StudyGroupId, cancellationToken);

        string email = dto.Email.Trim();
        if (!string.Equals(student.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            await userProvisioningService.EnsureEmailAvailableAsync(email, id, cancellationToken);

            student.Email = email;
            student.UserName = email;
            student.NormalizedEmail = email.ToUpperInvariant();
            student.NormalizedUserName = email.ToUpperInvariant();
        }

        student.FullName = dto.FullName.Trim();
        student.DefenceSessionId = dto.DefenceSessionId;
        student.StudyGroupId = dto.StudyGroupId;

        IdentityResult result = await userManager.UpdateAsync(student);
        if (!result.Succeeded)
        {
            string details = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new DomainException($"Failed to update student: {details}");
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplicationUser? student = await FindStudentAsync(id, asNoTracking: false, cancellationToken);
        if (student is null)
        {
            throw new DomainException("Student not found.");
        }

        bool hasDiploma = await dbContext.Diplomas
            .AnyAsync(diploma => diploma.StudentId == id, cancellationToken);

        if (hasDiploma)
        {
            throw new DomainException("Cannot delete a student linked to diplomas.");
        }

        IdentityResult result = await userManager.DeleteAsync(student);
        if (!result.Succeeded)
        {
            string details = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new DomainException($"Failed to delete student: {details}");
        }
    }

    private static StudentListItemDto MapListItem(
        ApplicationUser student,
        Dictionary<Guid, string> groupNames,
        Dictionary<Guid, DefenceSession> sessions)
    {
        string studyGroupName = student.StudyGroupId is Guid groupId && groupNames.TryGetValue(groupId, out string? name)
            ? name
            : "—";

        Guid defenceSessionId = student.DefenceSessionId ?? Guid.Empty;
        sessions.TryGetValue(defenceSessionId, out DefenceSession? session);

        return new StudentListItemDto(
            student.Id,
            student.FullName,
            student.Email ?? string.Empty,
            defenceSessionId,
            FormatSessionLabel(session),
            studyGroupName,
            student.CreatedAt);
    }

    private static string FormatSessionLabel(DefenceSession? session) =>
        session is null
            ? "—"
            : SecretarySessionLabel.Format(session.Year, session.Type, session.Semester);

    private async Task<ApplicationUser?> FindStudentAsync(
        Guid id,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        IQueryable<ApplicationUser> query = dbContext.Users
            .Where(user => user.Id == id && user.UserKind == UserKind.Student);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task EnsureStudentSessionAndGroupAsync(
        Guid defenceSessionId,
        Guid studyGroupId,
        CancellationToken cancellationToken)
    {
        Guid departmentId = await currentDepartmentResolver.ResolveRequiredScopedDepartmentIdAsync(cancellationToken);

        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(defenceSession => defenceSession.Id == defenceSessionId, cancellationToken);

        if (session is null)
        {
            throw new DomainException("Defence session not found.");
        }

        if (session.DepartmentId != departmentId)
        {
            throw new DomainException(DepartmentMessages.SessionNotInDepartment);
        }

        if (session.Status == DefenceSessionStatus.Archived)
        {
            throw new DomainException("Defence session is archived.");
        }

        StudyGroup? group = await dbContext.StudyGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(studyGroup => studyGroup.Id == studyGroupId, cancellationToken);

        if (group is null)
        {
            throw new DomainException("Study group not found.");
        }

        if (group.DefenceSessionId != defenceSessionId)
        {
            throw new DomainException("Study group does not belong to the selected defence session.");
        }
    }

    private async Task EnsureDiplomaAsync(
        Guid studentId,
        Guid defenceSessionId,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> existingStudentIds = await dbContext.Diplomas
            .Where(diploma => diploma.DefenceSessionId == defenceSessionId)
            .Select(diploma => diploma.StudentId)
            .ToHashSetAsync(cancellationToken);

        IReadOnlyList<Diploma> newDiplomas = diplomaCreationService.CreateForStudents(
            [new DiplomaStudentCandidate(studentId, UserKind.Student)],
            defenceSessionId,
            existingStudentIds);

        if (newDiplomas.Count == 0)
        {
            return;
        }

        dbContext.Diplomas.AddRange(newDiplomas);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
