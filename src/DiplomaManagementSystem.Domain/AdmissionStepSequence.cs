using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain;

public static class AdmissionStepSequence
{
    public static IReadOnlyList<AdmissionStep> WorkflowOrder { get; } =
    [
        AdmissionStep.SupervisorFeedback,
        AdmissionStep.FormattingReview,
        AdmissionStep.AntiPlagiarismClearance,
        AdmissionStep.ReviewerAssignment,
        AdmissionStep.ExternalReview,
    ];

    public static IReadOnlyList<AdmissionStep> OutcomeSteps { get; } =
    [
        AdmissionStep.SupervisorFeedback,
        AdmissionStep.FormattingReview,
        AdmissionStep.AntiPlagiarismClearance,
        AdmissionStep.ExternalReview,
    ];

    public static bool AcceptsOutcome(AdmissionStep step) =>
        step != AdmissionStep.ReviewerAssignment;

    public static bool ArePriorOutcomeStepsPassing(
        AdmissionStep step,
        IEnumerable<DiplomaAdmissionStepAttempt> attempts)
    {
        int index = OutcomeSteps.ToList().IndexOf(step);
        if (index <= 0)
        {
            return true;
        }

        for (int stepIndex = 0; stepIndex < index; stepIndex++)
        {
            AdmissionStep priorStep = OutcomeSteps[stepIndex];
            if (!AdmissionStepStatusResolver.HasPassingAttempt(priorStep, attempts))
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsReadyForReviewerAssignment(IEnumerable<DiplomaAdmissionStepAttempt> attempts) =>
        OutcomeSteps
            .Take(3)
            .All(step => AdmissionStepStatusResolver.HasPassingAttempt(step, attempts));

    public static AdmissionStep? GetNextStep(AdmissionStep step)
    {
        var order = WorkflowOrder.ToList();
        int index = order.IndexOf(step);
        if (index < 0 || index >= order.Count - 1)
        {
            return null;
        }

        return order[index + 1];
    }
}
