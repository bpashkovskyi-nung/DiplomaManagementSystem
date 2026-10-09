using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Domain.Services;

public sealed class AdmissionWorkflowService
{
    public void StartAdmissionReview(
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
            throw new DomainException("Admission review has already started.");
        }

        if (diploma.LifecycleStatus != DiplomaLifecycleStatus.WorkInProgressByStudent)
        {
            throw new DomainException("Work is not in the student execution phase.");
        }

        diploma.CurrentAdmissionStep = AdmissionStep.SupervisorFeedback;
        diploma.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public DiplomaAdmissionStepAttempt RecordAttempt(
        Diploma diploma,
        AdmissionStep step,
        ICollection<DiplomaAdmissionStepAttempt> attempts,
        Guid recordedById,
        CheckpointOutcome outcome,
        string? comment,
        bool isSecretaryOverride = false)
    {
        ArgumentNullException.ThrowIfNull(diploma);
        ArgumentNullException.ThrowIfNull(attempts);

        if (!AdmissionStepSequence.AcceptsOutcome(step))
        {
            throw new DomainException("This admission step does not accept review outcomes.");
        }

        if (isSecretaryOverride)
        {
            EnsureSecretaryOverrideAllowed(diploma, step, attempts);
        }
        else
        {
            if (diploma.CurrentAdmissionStep != step)
            {
                throw new DomainException("This is not the current admission step.");
            }

            if (!AdmissionStepSequence.ArePriorOutcomeStepsPassing(step, attempts))
            {
                throw new DomainException("Previous admission steps must be completed first.");
            }

            if (step == AdmissionStep.ExternalReview
                && diploma.ReviewAssignmentStatus != ReviewAssignmentStatus.Assigned)
            {
                throw new DomainException("Review assignment is not active.");
            }

            DiplomaAdmissionStepAttempt? lastAttempt = AdmissionStepStatusResolver.GetLastAttempt(step, attempts);
            if (lastAttempt is not null
                && CheckpointOutcomeRules.IsPassing(lastAttempt.Outcome))
            {
                throw new DomainException("This admission step is already completed.");
            }
        }

        if (CheckpointOutcomeRules.RequiresComment(outcome) && string.IsNullOrWhiteSpace(comment))
        {
            throw new DomainException("Comment is required for this admission step outcome.");
        }

        int attemptNumber = attempts.Count(attempt => attempt.Step == step) + 1;

        DiplomaAdmissionStepAttempt attempt = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diploma.Id,
            Step = step,
            AttemptNumber = attemptNumber,
            Outcome = outcome,
            Comment = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim(),
            RecordedById = recordedById,
            RecordedAt = DateTimeOffset.UtcNow,
            IsSecretaryOverride = isSecretaryOverride,
        };

        attempts.Add(attempt);

        if (step == AdmissionStep.ExternalReview && CheckpointOutcomeRules.IsPassing(outcome))
        {
            diploma.ReviewAssignmentStatus = ReviewAssignmentStatus.Completed;
        }

        diploma.CurrentAdmissionStep = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, attempts);
        diploma.UpdatedAt = DateTimeOffset.UtcNow;

        return attempt;
    }

    public void AdvanceAfterReviewerAssignment(Diploma diploma, IEnumerable<DiplomaAdmissionStepAttempt> attempts)
    {
        ArgumentNullException.ThrowIfNull(diploma);
        ArgumentNullException.ThrowIfNull(attempts);

        if (diploma.CurrentAdmissionStep != AdmissionStep.ReviewerAssignment
            && diploma.ReviewAssignmentStatus != ReviewAssignmentStatus.Assigned)
        {
            diploma.CurrentAdmissionStep = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, attempts);
        }
        else if (diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.Assigned)
        {
            diploma.CurrentAdmissionStep = AdmissionStep.ExternalReview;
        }

        diploma.UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void EnsureSecretaryOverrideAllowed(
        Diploma diploma,
        AdmissionStep step,
        ICollection<DiplomaAdmissionStepAttempt> attempts)
    {
        if (diploma.CurrentAdmissionStep != step)
        {
            throw new DomainException("Secretary override is allowed only for the current waiting admission step.");
        }

        if (!AdmissionStepStatusResolver.CanSecretaryOverrideCurrentStep(diploma, attempts))
        {
            throw new DomainException("The current admission step cannot be overridden.");
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
