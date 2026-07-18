using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Domain.Services;

public sealed class WorkReadinessService
{
    public void DeclareReady(
        Diploma diploma,
        DefenceSession defenceSession,
        DiplomaTopicVersion latestTopic,
        IEnumerable<DiplomaAdmissionStepAttempt> existingAttempts)
    {
        ArgumentNullException.ThrowIfNull(diploma);
        ArgumentNullException.ThrowIfNull(defenceSession);
        ArgumentNullException.ThrowIfNull(latestTopic);
        ArgumentNullException.ThrowIfNull(existingAttempts);

        EnsureSessionWritable(defenceSession);

        if (latestTopic.Status != TopicVersionStatus.Approved)
        {
            throw new DomainException("Topic must be approved before declaring work readiness.");
        }

        if (existingAttempts.Any())
        {
            throw new DomainException("Admission checks have already started.");
        }

        if (diploma.LifecycleStatus != DiplomaLifecycleStatus.ReviewerAssigned)
        {
            throw new DomainException("Work is not in the student execution phase.");
        }
    }

    private static void EnsureSessionWritable(DefenceSession defenceSession)
    {
        if (defenceSession.Status == DefenceSessionStatus.Archived)
        {
            throw new DomainException("Defence session is archived.");
        }
    }
}
