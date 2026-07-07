using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class AdmissionReadinessServiceTests
{
    private readonly AdmissionReadinessService _service = new();

    [Fact]
    public void IsReadyForAdmission_WhenTopicNull_ReturnsFalse()
    {
        Diploma diploma = new()
        {
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
            CurrentAdmissionStep = null,
        };

        bool result = _service.IsReadyForAdmission(diploma, null, CreateAllPassingAttempts());

        Assert.False(result);
    }

    [Fact]
    public void IsReadyForAdmission_WhenTopicNotApproved_ReturnsFalse()
    {
        Diploma diploma = new()
        {
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
        };

        DiplomaTopicVersion topic = new() { Status = TopicVersionStatus.PendingSupervisor };

        bool result = _service.IsReadyForAdmission(diploma, topic, CreateAllPassingAttempts());

        Assert.False(result);
    }

    [Fact]
    public void IsReadyForAdmission_WhenReviewNotCompleted_ReturnsFalse()
    {
        Diploma diploma = new()
        {
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned,
        };

        DiplomaTopicVersion topic = new() { Status = TopicVersionStatus.Approved };

        bool result = _service.IsReadyForAdmission(diploma, topic, CreateAllPassingAttempts());

        Assert.False(result);
    }

    [Fact]
    public void IsReadyForAdmission_WhenCurrentStepNotNull_ReturnsFalse()
    {
        Diploma diploma = new()
        {
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
            CurrentAdmissionStep = AdmissionStep.SupervisorFeedback,
        };

        DiplomaTopicVersion topic = new() { Status = TopicVersionStatus.Approved };

        bool result = _service.IsReadyForAdmission(diploma, topic, CreateAllPassingAttempts());

        Assert.False(result);
    }

    [Fact]
    public void IsReadyForAdmission_WhenAllConditionsMet_ReturnsTrue()
    {
        Diploma diploma = new()
        {
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
            CurrentAdmissionStep = null,
        };

        DiplomaTopicVersion topic = new() { Status = TopicVersionStatus.Approved };

        List<DiplomaAdmissionStepAttempt> attempts = CreateAllPassingAttempts();

        bool result = _service.IsReadyForAdmission(diploma, topic, attempts);

        Assert.True(result);
    }

    [Fact]
    public void IsReadyForAdmission_WhenSupervisorNotConfirmed_ReturnsFalse()
    {
        Diploma diploma = new()
        {
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Pending,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
        };

        DiplomaTopicVersion topic = new() { Status = TopicVersionStatus.Approved };

        List<DiplomaAdmissionStepAttempt> attempts = CreateAllPassingAttempts();

        bool result = _service.IsReadyForAdmission(diploma, topic, attempts);

        Assert.False(result);
    }

    [Fact]
    public void IsReadyForAdmission_WhenFormattingNotApproved_ReturnsFalse()
    {
        Diploma diploma = new()
        {
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
        };

        DiplomaTopicVersion topic = new() { Status = TopicVersionStatus.Approved };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreatePassing(AdmissionStep.SupervisorFeedback),
            CreatePassing(AdmissionStep.ExternalReview),
            CreatePassing(AdmissionStep.AntiPlagiarismClearance),
            new()
            {
                Step = AdmissionStep.FormattingReview,
                AttemptNumber = 1,
                Outcome = CheckpointOutcome.NotApproved,
                Comment = "Помилки оформлення",
            },
        ];

        bool result = _service.IsReadyForAdmission(diploma, topic, attempts);

        Assert.False(result);
    }

    private static DiplomaAdmissionStepAttempt CreatePassing(
        AdmissionStep step,
        CheckpointOutcome outcome = CheckpointOutcome.Approved)
    {
        return new DiplomaAdmissionStepAttempt
        {
            Step = step,
            AttemptNumber = 1,
            Outcome = outcome,
        };
    }

    private static List<DiplomaAdmissionStepAttempt> CreateAllPassingAttempts()
    {
        return
        [
            CreatePassing(AdmissionStep.SupervisorFeedback),
            CreatePassing(AdmissionStep.FormattingReview),
            CreatePassing(AdmissionStep.AntiPlagiarismClearance),
            CreatePassing(AdmissionStep.ExternalReview),
        ];
    }
}
