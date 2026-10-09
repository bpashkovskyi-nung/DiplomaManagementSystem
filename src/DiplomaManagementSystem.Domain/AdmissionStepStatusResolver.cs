using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain;

public static class AdmissionStepStatusResolver
{
    public static bool HasPassingAttempt(AdmissionStep step, IEnumerable<DiplomaAdmissionStepAttempt> attempts) =>
        attempts
            .Where(attempt => attempt.Step == step)
            .Any(attempt => CheckpointOutcomeRules.IsPassing(attempt.Outcome));

    public static DiplomaAdmissionStepAttempt? GetLastAttempt(
        AdmissionStep step,
        IEnumerable<DiplomaAdmissionStepAttempt> attempts) =>
        attempts
            .Where(attempt => attempt.Step == step)
            .OrderByDescending(attempt => attempt.AttemptNumber)
            .ThenByDescending(attempt => attempt.RecordedAt)
            .FirstOrDefault();

    public static DiplomaAdmissionStepAttempt? GetLastPassingAttempt(
        AdmissionStep step,
        IEnumerable<DiplomaAdmissionStepAttempt> attempts) =>
        attempts
            .Where(attempt => attempt.Step == step && CheckpointOutcomeRules.IsPassing(attempt.Outcome))
            .OrderByDescending(attempt => attempt.AttemptNumber)
            .ThenByDescending(attempt => attempt.RecordedAt)
            .FirstOrDefault();

    public static bool IsStepActionable(AdmissionStep step, IEnumerable<DiplomaAdmissionStepAttempt> attempts)
    {
        if (!AdmissionStepSequence.AcceptsOutcome(step))
        {
            return false;
        }

        DiplomaAdmissionStepAttempt? lastAttempt = GetLastAttempt(step, attempts);
        if (lastAttempt is null)
        {
            return true;
        }

        return !CheckpointOutcomeRules.IsPassing(lastAttempt.Outcome);
    }

    public static bool CanSecretaryOverrideCurrentStep(
        Diploma diploma,
        IEnumerable<DiplomaAdmissionStepAttempt> attempts)
    {
        ArgumentNullException.ThrowIfNull(diploma);
        ArgumentNullException.ThrowIfNull(attempts);

        AdmissionStep? step = diploma.CurrentAdmissionStep;
        if (step is null || !AdmissionStepSequence.AcceptsOutcome(step.Value))
        {
            return false;
        }

        var attemptList = attempts.ToList();
        if (!IsStepActionable(step.Value, attemptList))
        {
            return false;
        }

        if (!AdmissionStepSequence.ArePriorOutcomeStepsPassing(step.Value, attemptList))
        {
            return false;
        }

        if (step == AdmissionStep.ExternalReview
            && diploma.ReviewAssignmentStatus != ReviewAssignmentStatus.Assigned)
        {
            return false;
        }

        return true;
    }

    public static bool AreAllOutcomeStepsPassing(IEnumerable<DiplomaAdmissionStepAttempt> attempts) =>
        AdmissionStepSequence.OutcomeSteps.All(step => HasPassingAttempt(step, attempts));

    public static AdmissionStep? ResolveCurrentStep(
        Diploma diploma,
        IEnumerable<DiplomaAdmissionStepAttempt> attempts)
    {
        var attemptList = attempts.ToList();

        foreach (AdmissionStep step in AdmissionStepSequence.WorkflowOrder)
        {
            if (step == AdmissionStep.ReviewerAssignment)
            {
                if (diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.NotAssigned)
                {
                    return AdmissionStep.ReviewerAssignment;
                }

                if (diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.Assigned
                    && !HasPassingAttempt(AdmissionStep.ExternalReview, attemptList))
                {
                    return AdmissionStep.ExternalReview;
                }

                continue;
            }

            if (!HasPassingAttempt(step, attemptList))
            {
                return step;
            }
        }

        return null;
    }
}
