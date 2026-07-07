using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class AdmissionWorkflowServiceTests
{
    private readonly AdmissionWorkflowService _service = new();

    [Fact]
    public void StartAdmissionReview_WhenTopicNotApproved_Throws()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.WorkInProgressByStudent,
        };

        Assert.Throws<DomainException>(() =>
            _service.StartAdmissionReview(
                diploma,
                CreateSession(),
                new DiplomaTopicVersion { Status = TopicVersionStatus.PendingSupervisor },
                []));
    }

    [Fact]
    public void StartAdmissionReview_WhenWrongLifecycle_Throws()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.DocumentsInProgress,
        };

        Assert.Throws<DomainException>(() =>
            _service.StartAdmissionReview(
                diploma,
                CreateSession(),
                new DiplomaTopicVersion { Status = TopicVersionStatus.Approved },
                []));
    }

    [Fact]
    public void StartAdmissionReview_SetsCurrentStepToSupervisorFeedback()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.WorkInProgressByStudent,
        };

        DiplomaTopicVersion topic = new() { Status = TopicVersionStatus.Approved };

        _service.StartAdmissionReview(diploma, CreateSession(), topic, []);

        Assert.Equal(AdmissionStep.SupervisorFeedback, diploma.CurrentAdmissionStep);
    }

    [Fact]
    public void StartAdmissionReview_WhenAttemptsExist_Throws()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.WorkInProgressByStudent,
        };

        Assert.Throws<DomainException>(() =>
            _service.StartAdmissionReview(
                diploma,
                CreateSession(),
                new DiplomaTopicVersion { Status = TopicVersionStatus.Approved },
                [new DiplomaAdmissionStepAttempt()]));
    }

    [Fact]
    public void StartAdmissionReview_WhenSessionArchived_Throws()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.WorkInProgressByStudent,
        };

        DefenceSession session = CreateSession();
        session.Status = DefenceSessionStatus.Archived;

        Assert.Throws<DomainException>(() =>
            _service.StartAdmissionReview(
                diploma,
                session,
                new DiplomaTopicVersion { Status = TopicVersionStatus.Approved },
                []));
    }

    [Fact]
    public void RecordAttempt_WhenWrongCurrentStep_Throws()
    {
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.FormattingReview };
        List<DiplomaAdmissionStepAttempt> attempts = [];

        Assert.Throws<DomainException>(() =>
            _service.RecordAttempt(
                diploma,
                AdmissionStep.SupervisorFeedback,
                attempts,
                Guid.NewGuid(),
                CheckpointOutcome.Approved,
                null));
    }

    [Fact]
    public void RecordAttempt_ReviewerAssignment_Throws()
    {
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.ReviewerAssignment };
        List<DiplomaAdmissionStepAttempt> attempts = [];

        Assert.Throws<DomainException>(() =>
            _service.RecordAttempt(
                diploma,
                AdmissionStep.ReviewerAssignment,
                attempts,
                Guid.NewGuid(),
                CheckpointOutcome.Approved,
                null));
    }

    [Fact]
    public void RecordAttempt_ExternalReviewApproved_SetsReviewCompleted()
    {
        Guid diplomaId = Guid.NewGuid();
        Guid reviewerId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            CurrentAdmissionStep = AdmissionStep.ExternalReview,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts = CreateFirstThreePassing();

        _service.RecordAttempt(
            diploma,
            AdmissionStep.ExternalReview,
            attempts,
            reviewerId,
            CheckpointOutcome.Approved,
            "OK");

        Assert.Equal(ReviewAssignmentStatus.Completed, diploma.ReviewAssignmentStatus);
        Assert.Null(diploma.CurrentAdmissionStep);
    }

    [Fact]
    public void AdvanceAfterReviewerAssignment_AtReviewerAssignmentNotAssigned_ResolvesStep()
    {
        Diploma diploma = new()
        {
            CurrentAdmissionStep = AdmissionStep.ReviewerAssignment,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        DateTimeOffset before = DateTimeOffset.UtcNow;
        _service.AdvanceAfterReviewerAssignment(diploma, CreateFirstThreePassing());

        Assert.Equal(AdmissionStep.ReviewerAssignment, diploma.CurrentAdmissionStep);
        Assert.True(diploma.UpdatedAt >= before);
    }

    [Fact]
    public void RecordAttempt_WhenPriorStepsIncomplete_Throws()
    {
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.FormattingReview };
        List<DiplomaAdmissionStepAttempt> attempts = [];

        Assert.Throws<DomainException>(() =>
            _service.RecordAttempt(
                diploma,
                AdmissionStep.FormattingReview,
                attempts,
                Guid.NewGuid(),
                CheckpointOutcome.Approved,
                null));
    }

    [Fact]
    public void RecordAttempt_WhenAlreadyPassing_Throws()
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
                null));
    }

    [Fact]
    public void RecordAttempt_WhenNotApproved_RequiresComment()
    {
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.SupervisorFeedback };
        List<DiplomaAdmissionStepAttempt> attempts = [];

        Assert.Throws<DomainException>(() =>
            _service.RecordAttempt(
                diploma,
                AdmissionStep.SupervisorFeedback,
                attempts,
                Guid.NewGuid(),
                CheckpointOutcome.NotApproved,
                " "));
    }

    [Fact]
    public void RecordAttempt_Approved_AddsAttemptAndAdvancesStep()
    {
        Guid userId = Guid.NewGuid();
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.SupervisorFeedback };
        List<DiplomaAdmissionStepAttempt> attempts = [];

        DiplomaAdmissionStepAttempt attempt = _service.RecordAttempt(
            diploma,
            AdmissionStep.SupervisorFeedback,
            attempts,
            userId,
            CheckpointOutcome.Approved,
            null);

        Assert.Single(attempts);
        Assert.Equal(CheckpointOutcome.Approved, attempt.Outcome);
        Assert.Equal(userId, attempt.RecordedById);
        Assert.Equal(1, attempt.AttemptNumber);
        Assert.Equal(AdmissionStep.FormattingReview, diploma.CurrentAdmissionStep);
    }

    [Fact]
    public void RecordAttempt_WhenRejected_AllowsReReview()
    {
        Guid userId = Guid.NewGuid();
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.SupervisorFeedback };
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

        DiplomaAdmissionStepAttempt attempt = _service.RecordAttempt(
            diploma,
            AdmissionStep.SupervisorFeedback,
            attempts,
            userId,
            CheckpointOutcome.Approved,
            null);

        Assert.Equal(2, attempts.Count(a => a.Step == AdmissionStep.SupervisorFeedback));
        Assert.Equal(2, attempt.AttemptNumber);
        Assert.Equal(CheckpointOutcome.Approved, attempt.Outcome);
        Assert.Equal(userId, attempt.RecordedById);
    }

    [Fact]
    public void AdvanceAfterReviewerAssignment_WhenReviewerAssigned_SetsExternalReview()
    {
        Diploma diploma = new()
        {
            CurrentAdmissionStep = AdmissionStep.ReviewerAssignment,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned,
        };

        _service.AdvanceAfterReviewerAssignment(diploma, CreateFirstThreePassing());

        Assert.Equal(AdmissionStep.ExternalReview, diploma.CurrentAdmissionStep);
        Assert.NotEqual(default, diploma.UpdatedAt);
    }

    [Fact]
    public void AdvanceAfterReviewerAssignment_WhenNotAssigned_ResolvesCurrentStep()
    {
        Diploma diploma = new()
        {
            CurrentAdmissionStep = AdmissionStep.SupervisorFeedback,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        _service.AdvanceAfterReviewerAssignment(diploma, CreateFirstThreePassing());

        Assert.Equal(AdmissionStep.ReviewerAssignment, diploma.CurrentAdmissionStep);
    }

    [Fact]
    public void RecordAttempt_ExternalReview_WhenReviewerNotAssigned_Throws()
    {
        Diploma diploma = new()
        {
            CurrentAdmissionStep = AdmissionStep.ExternalReview,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts = CreateFirstThreePassing();

        Assert.Throws<DomainException>(() =>
            _service.RecordAttempt(
                diploma,
                AdmissionStep.ExternalReview,
                attempts,
                Guid.NewGuid(),
                CheckpointOutcome.Approved,
                "OK"));
    }

    private static List<DiplomaAdmissionStepAttempt> CreateFirstThreePassing() =>
    [
        new() { Step = AdmissionStep.SupervisorFeedback, AttemptNumber = 1, Outcome = CheckpointOutcome.Approved },
        new() { Step = AdmissionStep.FormattingReview, AttemptNumber = 1, Outcome = CheckpointOutcome.Approved },
        new() { Step = AdmissionStep.AntiPlagiarismClearance, AttemptNumber = 1, Outcome = CheckpointOutcome.Approved },
    ];

    private static DefenceSession CreateSession() => new()
    {
        Status = DefenceSessionStatus.Active,
    };
}
