using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Application.Audit.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Admin.DefenceSessions;

internal sealed class DefenceSessionService(
    IApplicationDbContext dbContext,
    DefenceSessionArchiveService archiveService,
    IAuditLogWriter auditLogWriter) : IDefenceSessionService
{
    public async Task<IReadOnlyList<DefenceSessionListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.DefenceSessions
            .AsNoTracking()
            .OrderByDescending(s => s.Year)
            .ThenBy(s => s.Type)
            .Select(s => new DefenceSessionListItemDto(
                s.Id,
                s.Year,
                s.Type,
                s.Semester,
                s.Status,
                s.StudyGroups.Count,
                s.Diplomas.Count))
            .ToListAsync(cancellationToken);
    }

    public async Task<DefenceSessionFormDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default)
    {
        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return session is null
            ? null
            : new DefenceSessionFormDto(session.Id, session.Year, session.Type, session.Semester);
    }

    public async Task<DefenceSessionDetailsDto?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .Include(s => s.StudyGroups)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (session is null)
        {
            return null;
        }

        List<Guid> groupIds = session.StudyGroups.Select(g => g.Id).ToList();
        Dictionary<Guid, int> studentCounts = await GetStudentCountsAsync(groupIds, session.Id, cancellationToken);

        IReadOnlyList<StudyGroupItemDto> groups = session.StudyGroups
            .OrderBy(g => g.Name)
            .Select(g => new StudyGroupItemDto(
                g.Id,
                g.Name,
                g.Course,
                studentCounts.GetValueOrDefault(g.Id)))
            .ToList();

        int diplomaCount = await dbContext.Diplomas.CountAsync(d => d.DefenceSessionId == id, cancellationToken);

        return new DefenceSessionDetailsDto(
            session.Id,
            session.Year,
            session.Type,
            session.Semester,
            session.Status,
            groups,
            diplomaCount);
    }

    public async Task<Guid> CreateAsync(DefenceSessionFormDto form, CancellationToken cancellationToken = default)
    {
        DefenceSession session = new()
        {
            Id = Guid.NewGuid(),
            Year = form.Year,
            Type = form.Type,
            Semester = form.Semester,
            Status = DefenceSessionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.DefenceSessions.Add(session);
        await dbContext.SaveChangesAsync(cancellationToken);

        return session.Id;
    }

    public async Task UpdateAsync(Guid id, DefenceSessionFormDto form, CancellationToken cancellationToken = default)
    {
        DefenceSession session = await dbContext.DefenceSessions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
                                 ?? throw new DomainException($"Defence session {id} not found.");

        if (session.Status == DefenceSessionStatus.Archived)
        {
            throw new DomainException("Cannot edit archived defence session.");
        }

        session.Year = form.Year;
        session.Type = form.Type;
        session.Semester = form.Semester;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(Guid id, Guid performedById, CancellationToken cancellationToken = default)
    {
        DefenceSession session = await dbContext.DefenceSessions
                                     .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
                                 ?? throw new DomainException($"Defence session {id} not found.");

        DefenceSessionStatus oldStatus = session.Status;
        archiveService.Archive(session);

        AuditLogEntry auditEntry = new(
            performedById,
            nameof(DefenceSession),
            session.Id,
            "Archive",
            oldStatus.ToString(),
            session.Status.ToString(),
            session.Id);

        await auditLogWriter.WriteAsync(auditEntry, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Dictionary<Guid, int>> GetStudentCountsAsync(
        List<Guid> groupIds,
        Guid defenceSessionId,
        CancellationToken cancellationToken)
    {
        if (groupIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserKind == UserKind.Student
                           && user.DefenceSessionId == defenceSessionId
                           && user.StudyGroupId != null
                           && groupIds.Contains(user.StudyGroupId.Value))
            .GroupBy(user => user.StudyGroupId!.Value)
            .Select(grouping => new { GroupId = grouping.Key, Count = grouping.Count() })
            .ToDictionaryAsync(item => item.GroupId, item => item.Count, cancellationToken);
    }
}
