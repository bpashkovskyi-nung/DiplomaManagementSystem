using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Domain.Services;

public sealed class DiplomaTopicService
{
    public bool CanSubmitNewVersion(IEnumerable<DiplomaTopicVersion> versions)
    {
        ArgumentNullException.ThrowIfNull(versions);

        var versionList = versions.ToList();

        if (versionList.Any(version => version.Status == TopicVersionStatus.Approved))
        {
            return false;
        }

        return !versionList.Any(version => version.Status is TopicVersionStatus.PendingSupervisor
            or TopicVersionStatus.PendingHead);
    }

    public void EnsureCanSubmitNewVersion(IEnumerable<DiplomaTopicVersion> versions)
    {
        ArgumentNullException.ThrowIfNull(versions);

        var versionList = versions.ToList();

        if (versionList.Any(version => version.Status == TopicVersionStatus.Approved))
        {
            throw new DomainException("Cannot submit a new topic version after one has been approved.");
        }

        if (versionList.Any(version => version.Status is TopicVersionStatus.PendingSupervisor
                or TopicVersionStatus.PendingHead))
        {
            throw new DomainException("A topic version is already under review.");
        }
    }

    public DiplomaTopicVersion CreateVersion(Guid diplomaId, string title, int nextVersionNumber)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Topic title is required.");
        }

        return new DiplomaTopicVersion
        {
            Id = Guid.NewGuid(),
            DiplomaId = diplomaId,
            VersionNumber = nextVersionNumber,
            Title = title.Trim(),
            Status = TopicVersionStatus.PendingSupervisor,
            SubmittedAt = DateTimeOffset.UtcNow,
        };
    }

    public int GetNextVersionNumber(IEnumerable<DiplomaTopicVersion> versions)
    {
        ArgumentNullException.ThrowIfNull(versions);

        return versions.Any() ? versions.Max(v => v.VersionNumber) + 1 : 1;
    }
}
