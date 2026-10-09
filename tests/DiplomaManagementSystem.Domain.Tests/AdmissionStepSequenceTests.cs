using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests;

public sealed class AdmissionStepSequenceTests
{
    [Fact]
    public void AcceptsOutcome_ReviewerAssignment_ReturnsFalse()
    {
        Assert.False(AdmissionStepSequence.AcceptsOutcome(AdmissionStep.ReviewerAssignment));
        Assert.True(AdmissionStepSequence.AcceptsOutcome(AdmissionStep.FormattingReview));
    }

    [Fact]
    public void ArePriorOutcomeStepsPassing_ForFirstStep_ReturnsTrue()
    {
        Assert.True(AdmissionStepSequence.ArePriorOutcomeStepsPassing(
            AdmissionStep.SupervisorFeedback,
            []));
    }

    [Fact]
    public void ArePriorOutcomeStepsPassing_ForFormatting_WhenSupervisorNotPassing_ReturnsFalse()
    {
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

        Assert.False(AdmissionStepSequence.ArePriorOutcomeStepsPassing(
            AdmissionStep.FormattingReview,
            attempts));
    }

    [Fact]
    public void ArePriorOutcomeStepsPassing_WhenPriorStepsPass_ReturnsTrue()
    {
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreatePassing(AdmissionStep.SupervisorFeedback),
        ];

        Assert.True(AdmissionStepSequence.ArePriorOutcomeStepsPassing(
            AdmissionStep.FormattingReview,
            attempts));
    }

    [Fact]
    public void IsReadyForReviewerAssignment_WhenMissingStep_ReturnsFalse()
    {
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreatePassing(AdmissionStep.SupervisorFeedback),
            CreatePassing(AdmissionStep.FormattingReview),
        ];

        Assert.False(AdmissionStepSequence.IsReadyForReviewerAssignment(attempts));
    }

    [Fact]
    public void IsReadyForReviewerAssignment_WhenAllThreePassing_ReturnsTrue()
    {
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreatePassing(AdmissionStep.SupervisorFeedback),
            CreatePassing(AdmissionStep.FormattingReview),
            CreatePassing(AdmissionStep.AntiPlagiarismClearance),
        ];

        Assert.True(AdmissionStepSequence.IsReadyForReviewerAssignment(attempts));
    }

    [Fact]
    public void GetNextStep_ReturnsFollowingStepInWorkflow()
    {
        Assert.Equal(
            AdmissionStep.FormattingReview,
            AdmissionStepSequence.GetNextStep(AdmissionStep.SupervisorFeedback));
    }

    [Fact]
    public void GetNextStep_LastStep_ReturnsNull()
    {
        Assert.Null(AdmissionStepSequence.GetNextStep(AdmissionStep.ExternalReview));
    }

    [Fact]
    public void GetNextStep_UnknownStep_ReturnsNull()
    {
        Assert.Null(AdmissionStepSequence.GetNextStep((AdmissionStep)999));
    }

    private static DiplomaAdmissionStepAttempt CreatePassing(AdmissionStep step) =>
        new()
        {
            Step = step,
            AttemptNumber = 1,
            Outcome = CheckpointOutcome.Approved,
        };
}

public sealed class WorkReadinessServiceTests
{
    private readonly WorkReadinessService _service = new();

    [Fact]
    public void DeclareReady_WhenTopicApprovedAndNoAttempts_Succeeds()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.WorkInProgressByStudent,
        };

        DiplomaTopicVersion topic = new()
        {
            Status = TopicVersionStatus.Approved,
        };

        _service.DeclareReady(diploma, CreateSession(), topic, []);
    }

    [Fact]
    public void DeclareReady_WhenAttemptsExist_Throws()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.WorkInProgressByStudent,
        };

        Assert.Throws<DomainException>(() =>
            _service.DeclareReady(
                diploma,
                CreateSession(),
                new DiplomaTopicVersion { Status = TopicVersionStatus.Approved },
                [new DiplomaAdmissionStepAttempt()]));
    }

    [Fact]
    public void DeclareReady_WhenSessionArchived_Throws()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.WorkInProgressByStudent,
        };

        DefenceSession session = CreateSession();
        session.Status = DefenceSessionStatus.Archived;

        Assert.Throws<DomainException>(() =>
            _service.DeclareReady(
                diploma,
                session,
                new DiplomaTopicVersion { Status = TopicVersionStatus.Approved },
                []));
    }

    [Fact]
    public void DeclareReady_WhenTopicNotApproved_Throws()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.WorkInProgressByStudent,
        };

        Assert.Throws<DomainException>(() =>
            _service.DeclareReady(
                diploma,
                CreateSession(),
                new DiplomaTopicVersion { Status = TopicVersionStatus.PendingSupervisor },
                []));
    }

    [Fact]
    public void DeclareReady_WhenWrongLifecycle_Throws()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.DocumentsInProgress,
        };

        Assert.Throws<DomainException>(() =>
            _service.DeclareReady(
                diploma,
                CreateSession(),
                new DiplomaTopicVersion { Status = TopicVersionStatus.Approved },
                []));
    }

    private static DefenceSession CreateSession() => new()
    {
        Status = DefenceSessionStatus.Active,
    };
}
