using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Domain.Services;

public sealed class ReviewerAssignmentService
{
    public void Assign(
        Diploma diploma,
        DefenceSession defenceSession,
        Guid reviewerId,
        IEnumerable<DiplomaAdmissionStepAttempt> attempts,
        bool hasApprovedTopic)
    {
        ArgumentNullException.ThrowIfNull(diploma);
        ArgumentNullException.ThrowIfNull(defenceSession);
        ArgumentNullException.ThrowIfNull(attempts);

        EnsureSessionWritable(defenceSession);

        if (diploma.AdmissionStatus == DiplomaAdmissionStatus.Admitted)
        {
            throw new DomainException("Cannot assign a reviewer to an admitted diploma.");
        }

        if (!hasApprovedTopic)
        {
            throw new DomainException("Reviewer can be assigned only after the topic is approved.");
        }

        if (diploma.LifecycleStatus != DiplomaLifecycleStatus.TopicApproved)
        {
            throw new DomainException("Reviewer can be assigned only when the topic is approved and work has not started.");
        }

        if (diploma.CurrentAdmissionStep is not null || attempts.Any())
        {
            throw new DomainException("Reviewer can be assigned only before admission checks start.");
        }

        if (diploma.ReviewAssignmentStatus != ReviewAssignmentStatus.NotAssigned)
        {
            throw new DomainException(
                diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.Completed
                    ? "Review is already completed."
                    : "Reviewer is already assigned.");
        }

        diploma.ReviewerId = reviewerId;
        diploma.ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned;
        diploma.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void EnsureSessionWritable(DefenceSession defenceSession)
    {
        if (defenceSession.Status == DefenceSessionStatus.Archived)
        {
            throw new DomainException("Defence session is archived.");
        }
    }
}
