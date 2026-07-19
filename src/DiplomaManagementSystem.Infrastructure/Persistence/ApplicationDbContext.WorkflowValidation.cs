using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Services;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DiplomaManagementSystem.Infrastructure.Persistence;

public sealed partial class ApplicationDbContext
{
    internal void ValidateWorkflowInvariants(DiplomaWorkflowInvariantValidator validator)
    {
        HashSet<Guid> diplomaIds = CollectDiplomaIdsForValidation();
        if (diplomaIds.Count == 0)
        {
            return;
        }

        foreach (Guid diplomaId in diplomaIds)
        {
            Diploma diploma = ResolveDiplomaForValidation(diplomaId);
            IReadOnlyList<DiplomaTopicVersion> topicVersions = ResolveTopicVersionsForValidation(diplomaId);
            validator.Validate(diploma, topicVersions);
        }
    }

    private HashSet<Guid> CollectDiplomaIdsForValidation()
    {
        HashSet<Guid> diplomaIds = [];

        foreach (EntityEntry<Diploma> entry in ChangeTracker.Entries<Diploma>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                diplomaIds.Add(entry.Entity.Id);
            }
        }

        foreach (EntityEntry<DiplomaTopicVersion> entry in ChangeTracker.Entries<DiplomaTopicVersion>())
        {
            if (entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                diplomaIds.Add(entry.Entity.DiplomaId);
            }
        }

        return diplomaIds;
    }

    private Diploma ResolveDiplomaForValidation(Guid diplomaId)
    {
        EntityEntry<Diploma>? trackedEntry = ChangeTracker.Entries<Diploma>()
            .FirstOrDefault(entry => entry.Entity.Id == diplomaId);

        if (trackedEntry is not null)
        {
            return trackedEntry.Entity;
        }

        return Diplomas.Local.FirstOrDefault(diploma => diploma.Id == diplomaId)
               ?? Diplomas.AsNoTracking().First(diploma => diploma.Id == diplomaId);
    }

    private IReadOnlyList<DiplomaTopicVersion> ResolveTopicVersionsForValidation(Guid diplomaId)
    {
        var trackedVersions = ChangeTracker.Entries<DiplomaTopicVersion>()
            .Where(entry => entry.Entity.DiplomaId == diplomaId && entry.State != EntityState.Deleted)
            .Select(entry => entry.Entity)
            .ToList();

        var trackedVersionIds = trackedVersions.Select(version => version.Id).ToHashSet();

        var persistedVersions = DiplomaTopicVersions
            .AsNoTracking()
            .Where(version => version.DiplomaId == diplomaId && !trackedVersionIds.Contains(version.Id))
            .ToList();

        return trackedVersions.Concat(persistedVersions).ToList();
    }
}
