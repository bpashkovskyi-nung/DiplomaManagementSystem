using DiplomaManagementSystem.Application.Authorization;
using DiplomaManagementSystem.Application.Authorization.Contracts;
using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Common.Contracts;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Employee;

internal sealed class SupervisorProgressService(
    IApplicationDbContext dbContext,
    IArchiveGuard archiveGuard,
    IDiplomaAuthorizationService diplomaAuthorizationService,
    IUserDisplayQueries userDisplayQueries,
    DefenceSessionMilestoneService milestoneService) : ISupervisorProgressService
{
    private const string MissingLabel = "—";

    public async Task<SupervisorProgressPageDto> GetPageAsync(
        Guid supervisorId,
        Guid? sessionId,
        CancellationToken cancellationToken = default)
    {
        List<Diploma> diplomas = await dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.DefenceSession)
            .Where(diploma => diploma.SupervisorId == supervisorId
                              && diploma.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Confirmed)
            .ToListAsync(cancellationToken);

        if (diplomas.Count == 0)
        {
            return new SupervisorProgressPageDto(Guid.Empty, string.Empty, [], []);
        }

        Guid selectedSessionId = sessionId
            ?? diplomas
                .OrderByDescending(diploma => diploma.DefenceSession.Year)
                .ThenByDescending(diploma => diploma.DefenceSession.CreatedAt)
                .Select(diploma => diploma.DefenceSessionId)
                .First();

        List<Diploma> sessionDiplomas = diplomas
            .Where(diploma => diploma.DefenceSessionId == selectedSessionId)
            .ToList();

        DefenceSession session = sessionDiplomas[0].DefenceSession;

        List<DefenceSessionMilestone> milestones = await LoadMilestonesAsync(selectedSessionId, cancellationToken);
        Dictionary<(Guid DiplomaId, Guid MilestoneId), int> progressByKey = await LoadProgressAsync(
            sessionDiplomas.Select(diploma => diploma.Id).ToHashSet(),
            cancellationToken);

        Dictionary<Guid, ApplicationUser> users = await userDisplayQueries.LoadUsersAsync(
            sessionDiplomas.Select(diploma => diploma.StudentId).ToHashSet(),
            cancellationToken);

        HashSet<Guid> groupIds = users.Values
            .Where(user => user.StudyGroupId.HasValue)
            .Select(user => user.StudyGroupId!.Value)
            .ToHashSet();

        Dictionary<Guid, string> groupNames = await userDisplayQueries.LoadStudyGroupNamesAsync(
            groupIds,
            cancellationToken);

        List<SupervisorProgressStudentDto> students = sessionDiplomas
            .Select(diploma =>
            {
                users.TryGetValue(diploma.StudentId, out ApplicationUser? student);
                string groupName = MissingLabel;
                if (student?.StudyGroupId is { } groupId
                    && groupNames.TryGetValue(groupId, out string? name))
                {
                    groupName = name;
                }

                return new SupervisorProgressStudentDto(
                    diploma.Id,
                    student?.FullName ?? MissingLabel,
                    groupName,
                    milestones.Select(milestone => new SupervisorProgressCellDto(
                        milestone.Id,
                        progressByKey.TryGetValue((diploma.Id, milestone.Id), out int percent)
                            ? percent
                            : null)).ToList());
            })
            .OrderBy(item => PersonNameSort.SurnameKey(item.StudentFullName), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.StudentFullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new SupervisorProgressPageDto(
            selectedSessionId,
            SecretarySessionLabel.Format(session.Year, session.Type, session.Semester),
            milestones.Select(MapMilestoneColumn).ToList(),
            students);
    }

    public async Task SetActualPercentAsync(
        Guid supervisorId,
        SetMilestoneProgressDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        milestoneService.ValidateActualPercent(request.ActualPercent);

        await diplomaAuthorizationService.EnsureCanPerformAsync(
            supervisorId,
            request.DiplomaId,
            DiplomaAction.RecordMilestoneProgress,
            cancellationToken);

        Diploma diploma = await dbContext.Diplomas
            .Include(item => item.DefenceSession)
            .FirstAsync(item => item.Id == request.DiplomaId, cancellationToken);

        archiveGuard.EnsureWritable(diploma.DefenceSession);
        milestoneService.EnsureSessionActive(diploma.DefenceSession);

        DefenceSessionMilestone? milestone = await dbContext.DefenceSessionMilestones
            .FirstOrDefaultAsync(
                item => item.Id == request.MilestoneId && item.DefenceSessionId == diploma.DefenceSessionId,
                cancellationToken);

        if (milestone is null)
        {
            throw new DomainException("Milestone was not found for this session.");
        }

        DiplomaMilestoneProgress? existing = await dbContext.DiplomaMilestoneProgressEntries
            .FirstOrDefaultAsync(
                item => item.DiplomaId == request.DiplomaId && item.MilestoneId == request.MilestoneId,
                cancellationToken);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        if (existing is null)
        {
            dbContext.DiplomaMilestoneProgressEntries.Add(new DiplomaMilestoneProgress
            {
                Id = Guid.NewGuid(),
                DiplomaId = request.DiplomaId,
                MilestoneId = request.MilestoneId,
                ActualPercent = request.ActualPercent,
                RecordedByUserId = supervisorId,
                RecordedAt = now,
            });
        }
        else
        {
            existing.ActualPercent = request.ActualPercent;
            existing.RecordedByUserId = supervisorId;
            existing.RecordedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<DefenceSessionMilestone>> LoadMilestonesAsync(
        Guid sessionId,
        CancellationToken cancellationToken) =>
        await dbContext.DefenceSessionMilestones
            .AsNoTracking()
            .Where(item => item.DefenceSessionId == sessionId)
            .OrderBy(item => item.Ordinal)
            .ToListAsync(cancellationToken);

    private async Task<Dictionary<(Guid DiplomaId, Guid MilestoneId), int>> LoadProgressAsync(
        HashSet<Guid> diplomaIds,
        CancellationToken cancellationToken)
    {
        if (diplomaIds.Count == 0)
        {
            return [];
        }

        List<DiplomaMilestoneProgress> rows = await dbContext.DiplomaMilestoneProgressEntries
            .AsNoTracking()
            .Where(item => diplomaIds.Contains(item.DiplomaId))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(item => (item.DiplomaId, item.MilestoneId), item => item.ActualPercent);
    }

    private static MilestoneColumnDto MapMilestoneColumn(DefenceSessionMilestone milestone) =>
        new(milestone.Id, milestone.Ordinal, milestone.DueDate, milestone.ExpectedPercent);
}
