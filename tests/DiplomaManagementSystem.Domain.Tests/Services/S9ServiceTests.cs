using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class AdmissionWorkflowServiceOverrideTests
{
    private readonly AdmissionWorkflowService _service = new();

    [Fact]
    public void Override_RecordAttempt_SetsSecretaryOverride()
    {
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.SupervisorFeedback };
        List<DiplomaAdmissionStepAttempt> attempts = [];
        var actorId = Guid.NewGuid();

        DiplomaAdmissionStepAttempt attempt = _service.RecordAttempt(
            diploma,
            AdmissionStep.SupervisorFeedback,
            attempts,
            actorId,
            CheckpointOutcome.Approved,
            null,
            isSecretaryOverride: true);

        Assert.True(attempt.IsSecretaryOverride);
        Assert.Equal(CheckpointOutcome.Approved, attempt.Outcome);
        Assert.Equal(actorId, attempt.RecordedById);
        Assert.Single(attempts);
    }

    [Fact]
    public void Override_WhenStepCannotBeOverridden_Throws()
    {
        Diploma diploma = new()
        {
            CurrentAdmissionStep = AdmissionStep.FormattingReview,
        };

        List<DiplomaAdmissionStepAttempt> attempts = [];

        Assert.Throws<DomainException>(() =>
            _service.RecordAttempt(
                diploma,
                AdmissionStep.FormattingReview,
                attempts,
                Guid.NewGuid(),
                CheckpointOutcome.Approved,
                null,
                isSecretaryOverride: true));
    }

    [Fact]
    public void Override_WhenNotCurrentStep_Throws()
    {
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.FormattingReview };
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            new()
            {
                Step = AdmissionStep.SupervisorFeedback,
                AttemptNumber = 1,
                Outcome = CheckpointOutcome.NotApproved,
                Comment = "Потрібні правки",
            },
        ];

        Assert.Throws<DomainException>(() =>
            _service.RecordAttempt(
                diploma,
                AdmissionStep.SupervisorFeedback,
                attempts,
                Guid.NewGuid(),
                CheckpointOutcome.Approved,
                null,
                isSecretaryOverride: true));
    }

    [Fact]
    public void Override_WhenStepAlreadyPassing_Throws()
    {
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.SupervisorFeedback };
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            new()
            {
                Step = AdmissionStep.SupervisorFeedback,
                AttemptNumber = 1,
                Outcome = CheckpointOutcome.Approved,
            },
        ];

        Assert.Throws<DomainException>(() =>
            _service.RecordAttempt(
                diploma,
                AdmissionStep.SupervisorFeedback,
                attempts,
                Guid.NewGuid(),
                CheckpointOutcome.Approved,
                null,
                isSecretaryOverride: true));
    }

    [Fact]
    public void Override_ExternalReview_WhenReviewerNotAssigned_Throws()
    {
        Diploma diploma = new()
        {
            CurrentAdmissionStep = AdmissionStep.ExternalReview,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            new() { Step = AdmissionStep.SupervisorFeedback, AttemptNumber = 1, Outcome = CheckpointOutcome.Approved },
            new() { Step = AdmissionStep.FormattingReview, AttemptNumber = 1, Outcome = CheckpointOutcome.Approved },
            new() { Step = AdmissionStep.AntiPlagiarismClearance, AttemptNumber = 1, Outcome = CheckpointOutcome.Approved },
        ];

        Assert.Throws<DomainException>(() =>
            _service.RecordAttempt(
                diploma,
                AdmissionStep.ExternalReview,
                attempts,
                Guid.NewGuid(),
                CheckpointOutcome.Approved,
                "Override",
                isSecretaryOverride: true));
    }
}

public sealed class DefenceSessionArchiveServiceTests
{
    private readonly DefenceSessionArchiveService _service = new();

    [Fact]
    public void Archive_SetsArchivedStatus()
    {
        DefenceSession session = new() { Status = DefenceSessionStatus.Active };

        _service.Archive(session);

        Assert.Equal(DefenceSessionStatus.Archived, session.Status);
        Assert.NotNull(session.ArchivedAt);
    }

    [Fact]
    public void Archive_WhenAlreadyArchived_Throws()
    {
        DefenceSession session = new() { Status = DefenceSessionStatus.Archived };

        Assert.Throws<DomainException>(() => _service.Archive(session));
    }
}
