using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Secretary;

internal sealed class DefenceDatePreferenceQueueService(
    IApplicationDbContext dbContext,
    IUserDisplayQueries userDisplayQueries) : IDefenceDatePreferenceQueueService
{
    private const string MissingLabel = "—";

    public async Task<DefenceDatePreferenceQueueDto?> GetQueueAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == sessionId, cancellationToken);

        if (session is null)
        {
            return null;
        }

        List<DateOnly> availableDates = await dbContext.DefenceDateOptions
            .AsNoTracking()
            .Where(item => item.DefenceSessionId == sessionId)
            .OrderBy(item => item.Date)
            .Select(item => item.Date)
            .ToListAsync(cancellationToken);

        var rows = await (
            from preference in dbContext.DefenceDatePreferences.AsNoTracking()
            join diploma in dbContext.Diplomas.AsNoTracking() on preference.DiplomaId equals diploma.Id
            join option in dbContext.DefenceDateOptions.AsNoTracking() on preference.DefenceDateOptionId equals option.Id
            where diploma.DefenceSessionId == sessionId
            orderby preference.RequestedAt
            select new
            {
                preference.DiplomaId,
                preference.RequesterType,
                preference.RequesterUserId,
                preference.RequestedAt,
                PreferredDate = option.Date,
                diploma.StudentId,
                ConfirmedDefenceDate = diploma.DefenceDate,
            }).ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return new DefenceDatePreferenceQueueDto(
                session.Id,
                SecretarySessionLabel.Format(session.Year, session.Type, session.Semester),
                [],
                availableDates);
        }

        HashSet<Guid> userIds = rows
            .Select(row => row.StudentId)
            .Concat(rows.Select(row => row.RequesterUserId))
            .ToHashSet();

        Dictionary<Guid, ApplicationUser> users = await userDisplayQueries.LoadUsersAsync(userIds, cancellationToken);

        HashSet<Guid> groupIds = users.Values
            .Where(user => user.StudyGroupId.HasValue)
            .Select(user => user.StudyGroupId!.Value)
            .ToHashSet();

        Dictionary<Guid, string> groupNames = await userDisplayQueries.LoadStudyGroupNamesAsync(
            groupIds,
            cancellationToken);

        List<DefenceDatePreferenceItemDto> items = rows.Select(row =>
        {
            users.TryGetValue(row.StudentId, out ApplicationUser? student);
            users.TryGetValue(row.RequesterUserId, out ApplicationUser? requester);

            string groupName = MissingLabel;
            if (student?.StudyGroupId is { } groupId
                && groupNames.TryGetValue(groupId, out string? name))
            {
                groupName = name;
            }

            return new DefenceDatePreferenceItemDto(
                row.DiplomaId,
                student?.FullName ?? MissingLabel,
                groupName,
                row.PreferredDate,
                row.RequestedAt,
                row.RequesterType == DefenceDateRequesterType.Student ? "Студент" : "Керівник",
                requester?.FullName ?? MissingLabel,
                row.ConfirmedDefenceDate);
        })
        .OrderBy(item => item.RequestedAt)
        .ThenBy(item => PersonNameSort.SurnameKey(item.StudentFullName), StringComparer.CurrentCultureIgnoreCase)
        .ToList();

        return new DefenceDatePreferenceQueueDto(
            session.Id,
            SecretarySessionLabel.Format(session.Year, session.Type, session.Semester),
            items,
            availableDates);
    }
}
