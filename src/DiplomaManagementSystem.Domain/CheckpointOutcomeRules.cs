using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain;

public static class CheckpointOutcomeRules
{
    public static bool IsPassing(CheckpointOutcome? outcome) =>
        outcome is CheckpointOutcome.Approved;

    public static bool RequiresComment(CheckpointOutcome outcome) =>
        outcome is CheckpointOutcome.NotApproved;

    public static bool RequiresDocument(CheckpointOutcome outcome) =>
        outcome is CheckpointOutcome.Approved;
}
