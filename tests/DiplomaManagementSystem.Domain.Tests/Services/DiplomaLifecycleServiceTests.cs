using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class DiplomaLifecycleServiceTests
{
    private readonly DiplomaLifecycleService _service = new(new AdmissionReadinessService());

    // TC-DOM-DLS-001
    [Fact]
    public void Recalculate_Admitted_ReturnsAdmitted()
    {
        Diploma diploma = new() { AdmissionStatus = DiplomaAdmissionStatus.Admitted };

        DiplomaLifecycleStatus status = _service.Recalculate(diploma, latestTopicVersion: null, attempts: []);

        Assert.Equal(DiplomaLifecycleStatus.Admitted, status);
    }

    // TC-DOM-DLS-002
    [Fact]
    public void Recalculate_ReadyForAdmission_ReturnsReady()
    {
        var diplomaId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DiplomaTopicVersion topic = new()
        {
            DiplomaId = diplomaId,
            Status = TopicVersionStatus.Approved,
            VersionNumber = 1,
            SubmittedAt = now,
        };

        Diploma diploma = new()
        {
            Id = diplomaId,
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
            CurrentAdmissionStep = null,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            CreatePassingAttempt(diplomaId, AdmissionStep.FormattingReview),
            CreatePassingAttempt(diplomaId, AdmissionStep.AntiPlagiarismClearance),
            CreatePassingAttempt(diplomaId, AdmissionStep.ExternalReview),
        ];

        DiplomaLifecycleStatus status = _service.Recalculate(diploma, topic, attempts);

        Assert.Equal(DiplomaLifecycleStatus.ReadyForAdmission, status);
    }

    // TC-DOM-DLS-003
    [Fact]
    public void Recalculate_WithAttempts_ReturnsDocumentsInProgress()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            CurrentAdmissionStep = AdmissionStep.SupervisorFeedback,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
        ];

        DiplomaLifecycleStatus status = _service.Recalculate(diploma, latestTopicVersion: null, attempts);

        Assert.Equal(DiplomaLifecycleStatus.DocumentsInProgress, status);
    }

    // TC-DOM-DLS-004
    [Fact]
    public void Recalculate_ApprovedTopicWithoutReviewer_ReturnsTopicApproved()
    {
        var diplomaId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        DiplomaTopicVersion topic = CreateApprovedTopic(diplomaId, supervisorId);

        Diploma diploma = new()
        {
            Id = diplomaId,
            SupervisorId = supervisorId,
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        DiplomaLifecycleStatus status = _service.Recalculate(diploma, topic, attempts: []);

        Assert.Equal(DiplomaLifecycleStatus.TopicApproved, status);
    }

    [Fact]
    public void Recalculate_ApprovedTopicWithReviewer_ReturnsReviewerAssigned()
    {
        var diplomaId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        DiplomaTopicVersion topic = CreateApprovedTopic(diplomaId, supervisorId);

        Diploma diploma = new()
        {
            Id = diplomaId,
            SupervisorId = supervisorId,
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewerId = Guid.NewGuid(),
            ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned,
        };

        DiplomaLifecycleStatus status = _service.Recalculate(diploma, topic, attempts: []);

        Assert.Equal(DiplomaLifecycleStatus.ReviewerAssigned, status);
    }

    // TC-DOM-DLS-005
    [Fact]
    public void Recalculate_PendingTopic_ReturnsTopicInReview()
    {
        var diplomaId = Guid.NewGuid();
        DiplomaTopicVersion topic = new()
        {
            DiplomaId = diplomaId,
            Status = TopicVersionStatus.PendingSupervisor,
            VersionNumber = 1,
            SubmittedAt = DateTimeOffset.UtcNow,
        };

        Diploma diploma = new() { Id = diplomaId };

        DiplomaLifecycleStatus status = _service.Recalculate(diploma, topic, attempts: []);

        Assert.Equal(DiplomaLifecycleStatus.TopicInReview, status);
    }

    // TC-DOM-DLS-006
    [Fact]
    public void Recalculate_SupervisorConfirmed_ReturnsSupervisorConfirmed()
    {
        Diploma diploma = new()
        {
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
        };

        DiplomaLifecycleStatus status = _service.Recalculate(diploma, latestTopicVersion: null, attempts: []);

        Assert.Equal(DiplomaLifecycleStatus.SupervisorConfirmed, status);
    }

    // TC-DOM-DLS-007
    [Fact]
    public void Recalculate_Default_ReturnsAwaitingSupervisor()
    {
        Diploma diploma = new()
        {
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Pending,
        };

        DiplomaLifecycleStatus status = _service.Recalculate(diploma, latestTopicVersion: null, attempts: []);

        Assert.Equal(DiplomaLifecycleStatus.AwaitingSupervisor, status);
    }

    // TC-DOM-DLS-008
    [Fact]
    public void CanStartAdmissionReview_Valid_ReturnsTrue()
    {
        bool canStart = _service.CanStartAdmissionReview(
            DiplomaLifecycleStatus.ReviewerAssigned,
            latestTopicVersion: new DiplomaTopicVersion { Status = TopicVersionStatus.Approved },
            attemptCount: 0,
            currentAdmissionStep: null);

        Assert.True(canStart);
    }

    // TC-DOM-DLS-009
    [Fact]
    public void CanStartAdmissionReview_WithAttempts_ReturnsFalse()
    {
        bool canStart = _service.CanStartAdmissionReview(
            DiplomaLifecycleStatus.ReviewerAssigned,
            latestTopicVersion: new DiplomaTopicVersion { Status = TopicVersionStatus.Approved },
            attemptCount: 1,
            currentAdmissionStep: AdmissionStep.SupervisorFeedback);

        Assert.False(canStart);
    }

    [Fact]
    public void CanStartAdmissionReview_TopicApproved_ReturnsFalse()
    {
        bool canStart = _service.CanStartAdmissionReview(
            DiplomaLifecycleStatus.TopicApproved,
            latestTopicVersion: new DiplomaTopicVersion { Status = TopicVersionStatus.Approved },
            attemptCount: 0,
            currentAdmissionStep: null);

        Assert.False(canStart);
    }

    private static DiplomaTopicVersion CreateApprovedTopic(Guid diplomaId, Guid supervisorId) =>
        new()
        {
            DiplomaId = diplomaId,
            Status = TopicVersionStatus.Approved,
            VersionNumber = 1,
            SubmittedAt = DateTimeOffset.UtcNow,
            SupervisorReviewedById = supervisorId,
            SupervisorReviewedAt = DateTimeOffset.UtcNow,
        };

    private static DiplomaAdmissionStepAttempt CreatePassingAttempt(Guid diplomaId, AdmissionStep step) =>
        new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diplomaId,
            Step = step,
            AttemptNumber = 1,
            Outcome = CheckpointOutcome.Approved,
        };
}
