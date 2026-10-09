using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain.Tests;

public sealed class AdmissionStepStatusResolverTests
{
    // TC-DOM-ASR-001
    [Fact]
    public void HasPassingAttempt_WhenApproved_ReturnsTrue()
    {
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved),
        ];

        Assert.True(AdmissionStepStatusResolver.HasPassingAttempt(AdmissionStep.SupervisorFeedback, attempts));
    }

    // TC-DOM-ASR-002
    [Fact]
    public void HasPassingAttempt_WhenOnlyRejected_ReturnsFalse()
    {
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.NotApproved),
        ];

        Assert.False(AdmissionStepStatusResolver.HasPassingAttempt(AdmissionStep.SupervisorFeedback, attempts));
    }

    // TC-DOM-ASR-003
    [Fact]
    public void GetLastAttempt_ReturnsHighestAttemptNumber()
    {
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.NotApproved, attemptNumber: 1),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.Approved, attemptNumber: 2),
        ];

        DiplomaAdmissionStepAttempt? last = AdmissionStepStatusResolver.GetLastAttempt(
            AdmissionStep.FormattingReview,
            attempts);

        Assert.NotNull(last);
        Assert.Equal(2, last.AttemptNumber);
    }

    // TC-DOM-ASR-004
    [Fact]
    public void GetLastPassingAttempt_SkipsRejected()
    {
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, attemptNumber: 1),
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.NotApproved, attemptNumber: 2),
        ];

        DiplomaAdmissionStepAttempt? lastPassing = AdmissionStepStatusResolver.GetLastPassingAttempt(
            AdmissionStep.SupervisorFeedback,
            attempts);

        Assert.NotNull(lastPassing);
        Assert.Equal(1, lastPassing.AttemptNumber);
    }

    // TC-DOM-ASR-005
    [Fact]
    public void IsStepActionable_NoAttempts_ReturnsTrue()
    {
        Assert.True(AdmissionStepStatusResolver.IsStepActionable(AdmissionStep.SupervisorFeedback, []));
    }

    // TC-DOM-ASR-006
    [Fact]
    public void IsStepActionable_LastPassing_ReturnsFalse()
    {
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved),
        ];

        Assert.False(AdmissionStepStatusResolver.IsStepActionable(AdmissionStep.SupervisorFeedback, attempts));
    }

    // TC-DOM-ASR-007
    [Fact]
    public void IsStepActionable_ReviewerAssignment_ReturnsFalse()
    {
        Assert.False(AdmissionStepStatusResolver.IsStepActionable(AdmissionStep.ReviewerAssignment, []));
    }

    // TC-DOM-ASR-008
    [Fact]
    public void IsStepActionable_LastRejected_ReturnsTrue()
    {
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.NotApproved),
        ];

        Assert.True(AdmissionStepStatusResolver.IsStepActionable(AdmissionStep.SupervisorFeedback, attempts));
    }

    // TC-DOM-ASR-009
    [Fact]
    public void CanSecretaryOverride_NoCurrentStep_ReturnsFalse()
    {
        Diploma diploma = new() { CurrentAdmissionStep = null };

        Assert.False(AdmissionStepStatusResolver.CanSecretaryOverrideCurrentStep(diploma, []));
    }

    // TC-DOM-ASR-010
    [Fact]
    public void CanSecretaryOverride_ReviewerAssignment_ReturnsFalse()
    {
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.ReviewerAssignment };

        Assert.False(AdmissionStepStatusResolver.CanSecretaryOverrideCurrentStep(diploma, []));
    }

    // TC-DOM-ASR-011
    [Fact]
    public void CanSecretaryOverride_ExternalWithoutReviewer_ReturnsFalse()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            CurrentAdmissionStep = AdmissionStep.ExternalReview,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.AntiPlagiarismClearance, CheckpointOutcome.Approved, diplomaId),
        ];

        Assert.False(AdmissionStepStatusResolver.CanSecretaryOverrideCurrentStep(diploma, attempts));
    }

    // TC-DOM-ASR-012
    [Fact]
    public void CanSecretaryOverride_ActionableStep_ReturnsTrue()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            CurrentAdmissionStep = AdmissionStep.FormattingReview,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
        ];

        Assert.True(AdmissionStepStatusResolver.CanSecretaryOverrideCurrentStep(diploma, attempts));
    }

    [Fact]
    public void CanSecretaryOverride_WhenPriorStepsIncomplete_ReturnsFalse()
    {
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.FormattingReview };

        Assert.False(AdmissionStepStatusResolver.CanSecretaryOverrideCurrentStep(diploma, []));
    }

    [Fact]
    public void GetLastAttempt_SameAttemptNumber_UsesRecordedAt()
    {
        DateTimeOffset earlier = DateTimeOffset.UtcNow.AddHours(-2);
        DateTimeOffset later = DateTimeOffset.UtcNow.AddHours(-1);
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            new()
            {
                Step = AdmissionStep.SupervisorFeedback,
                AttemptNumber = 1,
                Outcome = CheckpointOutcome.NotApproved,
                RecordedAt = earlier,
            },
            new()
            {
                Step = AdmissionStep.SupervisorFeedback,
                AttemptNumber = 1,
                Outcome = CheckpointOutcome.Approved,
                RecordedAt = later,
            },
        ];

        DiplomaAdmissionStepAttempt? last = AdmissionStepStatusResolver.GetLastAttempt(
            AdmissionStep.SupervisorFeedback,
            attempts);

        Assert.NotNull(last);
        Assert.Equal(later, last.RecordedAt);
    }

    // TC-DOM-ASR-013
    [Fact]
    public void AreAllOutcomeStepsPassing_AllPass_ReturnsTrue()
    {
        var diplomaId = Guid.NewGuid();
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.AntiPlagiarismClearance, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.ExternalReview, CheckpointOutcome.Approved, diplomaId),
        ];

        Assert.True(AdmissionStepStatusResolver.AreAllOutcomeStepsPassing(attempts));
    }

    // TC-DOM-ASR-014
    [Fact]
    public void AreAllOutcomeStepsPassing_MissingStep_ReturnsFalse()
    {
        var diplomaId = Guid.NewGuid();
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.AntiPlagiarismClearance, CheckpointOutcome.Approved, diplomaId),
        ];

        Assert.False(AdmissionStepStatusResolver.AreAllOutcomeStepsPassing(attempts));
    }

    // TC-DOM-ASR-015
    [Fact]
    public void ResolveCurrentStep_NoAttempts_ReturnsSupervisorFeedback()
    {
        Diploma diploma = new() { ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned };

        AdmissionStep? step = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, []);

        Assert.Equal(AdmissionStep.SupervisorFeedback, step);
    }

    [Fact]
    public void ResolveCurrentStep_SupervisorRejected_ReturnsSupervisorFeedback()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.NotApproved, diplomaId),
        ];

        AdmissionStep? step = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, attempts);

        Assert.Equal(AdmissionStep.SupervisorFeedback, step);
    }

    [Fact]
    public void ResolveCurrentStep_FormattingNotPassing_ReturnsFormattingReview()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.NotApproved, diplomaId),
        ];

        AdmissionStep? step = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, attempts);

        Assert.Equal(AdmissionStep.FormattingReview, step);
    }

    [Fact]
    public void ResolveCurrentStep_CompletedWithoutExternalReview_ReturnsExternalReview()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.AntiPlagiarismClearance, CheckpointOutcome.Approved, diplomaId),
        ];

        AdmissionStep? step = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, attempts);

        Assert.Equal(AdmissionStep.ExternalReview, step);
    }

    // TC-DOM-ASR-016
    [Fact]
    public void ResolveCurrentStep_ReadyForReviewer_ReturnsReviewerAssignment()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.AntiPlagiarismClearance, CheckpointOutcome.Approved, diplomaId),
        ];

        AdmissionStep? step = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, attempts);

        Assert.Equal(AdmissionStep.ReviewerAssignment, step);
    }

    // TC-DOM-ASR-017
    [Fact]
    public void ResolveCurrentStep_ReviewerAssigned_ReturnsExternalReview()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.AntiPlagiarismClearance, CheckpointOutcome.Approved, diplomaId),
        ];

        AdmissionStep? step = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, attempts);

        Assert.Equal(AdmissionStep.ExternalReview, step);
    }

    [Fact]
    public void ResolveCurrentStep_NotReadyForReviewer_SkipsReviewerAssignment()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.Approved, diplomaId),
        ];

        AdmissionStep? step = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, attempts);

        Assert.Equal(AdmissionStep.AntiPlagiarismClearance, step);
    }

    [Fact]
    public void ResolveCurrentStep_AssignedWithExternalPassing_ReturnsNull()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.AntiPlagiarismClearance, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.ExternalReview, CheckpointOutcome.Approved, diplomaId),
        ];

        AdmissionStep? step = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, attempts);

        Assert.Null(step);
    }

    // TC-DOM-ASR-018
    [Fact]
    public void ResolveCurrentStep_AllComplete_ReturnsNull()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreateAttempt(AdmissionStep.SupervisorFeedback, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.FormattingReview, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.AntiPlagiarismClearance, CheckpointOutcome.Approved, diplomaId),
            CreateAttempt(AdmissionStep.ExternalReview, CheckpointOutcome.Approved, diplomaId),
        ];

        AdmissionStep? step = AdmissionStepStatusResolver.ResolveCurrentStep(diploma, attempts);

        Assert.Null(step);
    }

    private static DiplomaAdmissionStepAttempt CreateAttempt(
        AdmissionStep step,
        CheckpointOutcome outcome,
        Guid? diplomaId = null,
        int attemptNumber = 1) =>
        new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diplomaId ?? Guid.NewGuid(),
            Step = step,
            AttemptNumber = attemptNumber,
            Outcome = outcome,
            RecordedAt = DateTimeOffset.UtcNow,
        };
}
