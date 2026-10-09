using DiplomaManagementSystem.Application.Common.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Secretary;

internal sealed class SessionSetupService(
    IApplicationDbContext dbContext,
    IArchiveGuard archiveGuard,
    DefenceSessionMilestoneService milestoneService,
    DefenceDatePreferenceService preferenceService) : ISessionSetupService
{
    public async Task<SessionSetupPageDto?> GetPageAsync(
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

        List<DefenceSessionMilestone> milestones = await dbContext.DefenceSessionMilestones
            .AsNoTracking()
            .Where(item => item.DefenceSessionId == sessionId)
            .OrderBy(item => item.Ordinal)
            .ToListAsync(cancellationToken);

        List<DefenceDateOption> options = await dbContext.DefenceDateOptions
            .AsNoTracking()
            .Where(item => item.DefenceSessionId == sessionId)
            .OrderBy(item => item.Date)
            .ToListAsync(cancellationToken);

        HashSet<Guid> protectedOptionIds = await GetProtectedOptionIdsAsync(sessionId, options, cancellationToken);

        List<MilestoneSetupItemDto> milestoneDtos = [];
        for (int ordinal = 1; ordinal <= DefenceSessionMilestoneService.RequiredMilestoneCount; ordinal++)
        {
            DefenceSessionMilestone? existing = milestones.FirstOrDefault(item => item.Ordinal == ordinal);
            milestoneDtos.Add(existing is null
                ? new MilestoneSetupItemDto(null, ordinal, null, null)
                : new MilestoneSetupItemDto(existing.Id, existing.Ordinal, existing.DueDate, existing.ExpectedPercent));
        }

        return new SessionSetupPageDto(
            session.Id,
            SecretarySessionLabel.Format(session.Year, session.Type, session.Semester),
            milestoneDtos,
            options.Select(option => new DefenceDateOptionDto(
                option.Id,
                option.Date,
                protectedOptionIds.Contains(option.Id))).ToList());
    }

    public async Task SaveMilestonesAsync(
        Guid sessionId,
        SaveMilestonesDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        DefenceSession session = await GetWritableSessionAsync(sessionId, cancellationToken);
        milestoneService.EnsureSessionActive(session);
        milestoneService.ValidateMilestones(
            request.Milestones.Select(item => (item.DueDate, item.ExpectedPercent)).ToList());

        List<DefenceSessionMilestone> existing = await dbContext.DefenceSessionMilestones
            .Where(item => item.DefenceSessionId == sessionId)
            .OrderBy(item => item.Ordinal)
            .ToListAsync(cancellationToken);

        for (int index = 0; index < request.Milestones.Count; index++)
        {
            int ordinal = index + 1;
            SaveMilestoneItemDto item = request.Milestones[index];
            DefenceSessionMilestone? current = existing.FirstOrDefault(entry => entry.Ordinal == ordinal);

            if (current is null)
            {
                dbContext.DefenceSessionMilestones.Add(new DefenceSessionMilestone
                {
                    Id = Guid.NewGuid(),
                    DefenceSessionId = sessionId,
                    Ordinal = ordinal,
                    DueDate = item.DueDate,
                    ExpectedPercent = item.ExpectedPercent,
                });
            }
            else
            {
                current.DueDate = item.DueDate;
                current.ExpectedPercent = item.ExpectedPercent;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveDefenceDatesAsync(
        Guid sessionId,
        SaveDefenceDatesDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        DefenceSession session = await GetWritableSessionAsync(sessionId, cancellationToken);
        milestoneService.EnsureSessionActive(session);

        List<DateOnly> distinctDates = request.Dates
            .Distinct()
            .OrderBy(date => date)
            .ToList();

        if (distinctDates.Count != request.Dates.Count)
        {
            throw new DomainException("Defence dates must be unique.");
        }

        List<DefenceDateOption> existing = await dbContext.DefenceDateOptions
            .Where(item => item.DefenceSessionId == sessionId)
            .ToListAsync(cancellationToken);

        HashSet<Guid> protectedOptionIds = await GetProtectedOptionIdsAsync(sessionId, existing, cancellationToken);

        foreach (DefenceDateOption option in existing.Where(item => !distinctDates.Contains(item.Date)).ToList())
        {
            if (protectedOptionIds.Contains(option.Id))
            {
                preferenceService.EnsureCanRemoveDateOption(option, hasPreferences: true, isAssignedAsFinalDate: false);
            }

            dbContext.DefenceDateOptions.Remove(option);
        }

        HashSet<DateOnly> existingDates = existing.Select(item => item.Date).ToHashSet();
        foreach (DateOnly date in distinctDates.Where(date => !existingDates.Contains(date)))
        {
            dbContext.DefenceDateOptions.Add(new DefenceDateOption
            {
                Id = Guid.NewGuid(),
                DefenceSessionId = sessionId,
                Date = date,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<DefenceSession> GetWritableSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        DefenceSession? session = await dbContext.DefenceSessions
            .FirstOrDefaultAsync(item => item.Id == sessionId, cancellationToken);

        if (session is null)
        {
            throw new DomainException("Defence session was not found.");
        }

        archiveGuard.EnsureWritable(session);
        return session;
    }

    private async Task<HashSet<Guid>> GetProtectedOptionIdsAsync(
        Guid sessionId,
        IReadOnlyCollection<DefenceDateOption> options,
        CancellationToken cancellationToken)
    {
        if (options.Count == 0)
        {
            return [];
        }

        HashSet<Guid> optionIds = options.Select(item => item.Id).ToHashSet();
        HashSet<DateOnly> optionDates = options.Select(item => item.Date).ToHashSet();

        List<Guid> preferredOptionIds = await dbContext.DefenceDatePreferences
            .AsNoTracking()
            .Where(item => optionIds.Contains(item.DefenceDateOptionId))
            .Select(item => item.DefenceDateOptionId)
            .Distinct()
            .ToListAsync(cancellationToken);

        List<DateOnly> confirmedDates = await dbContext.Diplomas
            .AsNoTracking()
            .Where(item => item.DefenceSessionId == sessionId
                           && item.DefenceDate != null
                           && optionDates.Contains(item.DefenceDate.Value))
            .Select(item => item.DefenceDate!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        HashSet<Guid> protectedIds = preferredOptionIds.ToHashSet();
        foreach (DefenceDateOption option in options.Where(item => confirmedDates.Contains(item.Date)))
        {
            protectedIds.Add(option.Id);
        }

        return protectedIds;
    }
}
