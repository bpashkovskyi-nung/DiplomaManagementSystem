using DiplomaManagementSystem.Application.Admin.StudyGroups.Contracts;
using DiplomaManagementSystem.Application.Admin.StudyGroups.Dtos;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Admin.StudyGroups;

internal sealed class StudyGroupAdminService(IApplicationDbContext dbContext) : IStudyGroupAdminService
{
    public async Task<IReadOnlyList<StudyGroupListItemDto>> GetAllAsync(
        Guid defenceSessionId,
        CancellationToken cancellationToken = default)
    {
        List<StudyGroup> groups = await dbContext.StudyGroups
            .AsNoTracking()
            .Where(group => group.DefenceSessionId == defenceSessionId)
            .OrderBy(group => group.Name)
            .ToListAsync(cancellationToken);

        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == defenceSessionId, cancellationToken);

        if (session is null)
        {
            throw new DomainException("Defence session not found.");
        }

        Dictionary<Guid, int> studentCounts = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserKind == UserKind.Student && user.StudyGroupId != null)
            .GroupBy(user => user.StudyGroupId!.Value)
            .Select(grouping => new { GroupId = grouping.Key, Count = grouping.Count() })
            .ToDictionaryAsync(item => item.GroupId, item => item.Count, cancellationToken);

        Dictionary<Guid, string> specialtyLabels = await dbContext.Specialties
            .AsNoTracking()
            .Where(specialty => groups.Select(group => group.SpecialtyId).Contains(specialty.Id))
            .ToDictionaryAsync(
                specialty => specialty.Id,
                specialty => specialty.Code + " — " + specialty.Name,
                cancellationToken);

        string sessionLabel = SecretarySessionLabel.Format(session.Year, session.Type, session.Semester);

        return groups
            .Select(group => new StudyGroupListItemDto(
                group.Id,
                group.Name,
                group.Course,
                group.DefenceSessionId,
                sessionLabel,
                specialtyLabels.GetValueOrDefault(group.SpecialtyId, "—"),
                group.StudyForm,
                studentCounts.GetValueOrDefault(group.Id)))
            .ToList();
    }

    public async Task<StudyGroupFormDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        StudyGroup? group = await dbContext.StudyGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(studyGroup => studyGroup.Id == id, cancellationToken);

        if (group is null)
        {
            return null;
        }

        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == group.DefenceSessionId, cancellationToken);

        string? sessionLabel = session is null
            ? null
            : SecretarySessionLabel.Format(session.Year, session.Type, session.Semester);

        return new StudyGroupFormDto(
            group.Id,
            group.DefenceSessionId,
            group.Name,
            group.SpecialtyId,
            group.StudyForm,
            group.Course,
            sessionLabel);
    }

    public async Task<StudyGroupListItemDto?> GetListItemAsync(Guid id, CancellationToken cancellationToken = default)
    {
        StudyGroup? group = await dbContext.StudyGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(studyGroup => studyGroup.Id == id, cancellationToken);

        if (group is null)
        {
            return null;
        }

        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == group.DefenceSessionId, cancellationToken);

        if (session is null)
        {
            return null;
        }

        Specialty? specialty = await dbContext.Specialties
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == group.SpecialtyId, cancellationToken);

        int studentCount = await dbContext.Users
            .AsNoTracking()
            .CountAsync(
                user => user.UserKind == UserKind.Student && user.StudyGroupId == id,
                cancellationToken);

        string sessionLabel = SecretarySessionLabel.Format(session.Year, session.Type, session.Semester);

        return new StudyGroupListItemDto(
            group.Id,
            group.Name,
            group.Course,
            group.DefenceSessionId,
            sessionLabel,
            specialty is null ? "—" : specialty.Code + " — " + specialty.Name,
            group.StudyForm,
            studentCount);
    }

    public async Task<Guid> CreateAsync(StudyGroupFormDto dto, CancellationToken cancellationToken = default)
    {
        await EnsureSessionAllowsChangesAsync(dto.DefenceSessionId, cancellationToken);
        await EnsureNameAvailableAsync(dto.Name, dto.DefenceSessionId, excludeId: null, cancellationToken);
        await EnsureSpecialtyBelongsToSessionDepartmentAsync(dto.DefenceSessionId, dto.SpecialtyId, cancellationToken);

        StudyGroup group = new()
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Course = dto.Course,
            DefenceSessionId = dto.DefenceSessionId,
            SpecialtyId = dto.SpecialtyId,
            StudyForm = dto.StudyForm.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.StudyGroups.Add(group);
        await dbContext.SaveChangesAsync(cancellationToken);
        return group.Id;
    }

    public async Task UpdateAsync(Guid id, StudyGroupFormDto dto, CancellationToken cancellationToken = default)
    {
        StudyGroup? group = await dbContext.StudyGroups
            .FirstOrDefaultAsync(studyGroup => studyGroup.Id == id, cancellationToken);

        if (group is null)
        {
            throw new DomainException("Study group not found.");
        }

        if (group.DefenceSessionId != dto.DefenceSessionId)
        {
            throw new DomainException("Cannot change defence session for an existing study group.");
        }

        await EnsureSessionAllowsChangesAsync(group.DefenceSessionId, cancellationToken);
        await EnsureNameAvailableAsync(dto.Name, group.DefenceSessionId, excludeId: id, cancellationToken);
        await EnsureSpecialtyBelongsToSessionDepartmentAsync(dto.DefenceSessionId, dto.SpecialtyId, cancellationToken);

        group.Name = dto.Name.Trim();
        group.Course = dto.Course;
        group.SpecialtyId = dto.SpecialtyId;
        group.StudyForm = dto.StudyForm.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        StudyGroup? group = await dbContext.StudyGroups
            .FirstOrDefaultAsync(studyGroup => studyGroup.Id == id, cancellationToken);

        if (group is null)
        {
            throw new DomainException("Study group not found.");
        }

        await EnsureSessionAllowsChangesAsync(group.DefenceSessionId, cancellationToken);

        bool hasStudents = await dbContext.Users
            .AnyAsync(
                user => user.UserKind == UserKind.Student && user.StudyGroupId == id,
                cancellationToken);

        if (hasStudents)
        {
            throw new DomainException("Cannot delete a study group that has students.");
        }

        dbContext.StudyGroups.Remove(group);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSessionAllowsChangesAsync(Guid defenceSessionId, CancellationToken cancellationToken)
    {
        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == defenceSessionId, cancellationToken);

        if (session is null)
        {
            throw new DomainException("Defence session not found.");
        }

        if (session.Status == DefenceSessionStatus.Archived)
        {
            throw new DomainException("Defence session is archived.");
        }
    }

    private async Task EnsureNameAvailableAsync(
        string name,
        Guid defenceSessionId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        string trimmedName = name.Trim();
        bool exists = await dbContext.StudyGroups
            .AnyAsync(
                group => group.Name == trimmedName
                         && group.DefenceSessionId == defenceSessionId
                         && (excludeId == null || group.Id != excludeId),
                cancellationToken);

        if (exists)
        {
            throw new DomainException($"Study group name is already in use ({trimmedName}).");
        }
    }

    private async Task EnsureSpecialtyBelongsToSessionDepartmentAsync(
        Guid defenceSessionId,
        Guid specialtyId,
        CancellationToken cancellationToken)
    {
        Guid? departmentId = await dbContext.DefenceSessions
            .AsNoTracking()
            .Where(session => session.Id == defenceSessionId)
            .Select(session => (Guid?)session.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);

        if (departmentId is not Guid id)
        {
            throw new DomainException("Defence session not found.");
        }

        bool belongs = await dbContext.Specialties
            .AsNoTracking()
            .AnyAsync(
                specialty => specialty.Id == specialtyId
                             && specialty.DepartmentId == id
                             && specialty.IsActive,
                cancellationToken);

        if (!belongs)
        {
            throw new DomainException("Оберіть дійсну спеціальність кафедри сесії.");
        }
    }
}
