using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class ReviewerAssignmentServiceTests
{
    private readonly ReviewerAssignmentService _service = new();

    [Fact]
    public void Assign_WhenAdmitted_Throws()
    {
        Diploma diploma = CreateTopicApprovedDiploma();
        diploma.AdmissionStatus = DiplomaAdmissionStatus.Admitted;

        Assert.Throws<DomainException>(() =>
            _service.Assign(diploma, CreateSession(), Guid.NewGuid(), [], hasApprovedTopic: true));
    }

    [Fact]
    public void Assign_WhenSessionArchived_Throws()
    {
        Diploma diploma = CreateTopicApprovedDiploma();

        DefenceSession session = CreateSession();
        session.Status = DefenceSessionStatus.Archived;

        Assert.Throws<DomainException>(() =>
            _service.Assign(diploma, session, Guid.NewGuid(), [], hasApprovedTopic: true));
    }

    [Fact]
    public void Assign_WhenTopicApproved_SetsReviewerWithoutAdmissionStep()
    {
        Diploma diploma = CreateTopicApprovedDiploma();

        var reviewerId = Guid.NewGuid();
        _service.Assign(diploma, CreateSession(), reviewerId, [], hasApprovedTopic: true);

        Assert.Equal(reviewerId, diploma.ReviewerId);
        Assert.Equal(ReviewAssignmentStatus.Assigned, diploma.ReviewAssignmentStatus);
        Assert.Null(diploma.CurrentAdmissionStep);
    }

    [Fact]
    public void Assign_WhenTopicNotApproved_Throws()
    {
        Diploma diploma = CreateTopicApprovedDiploma();

        Assert.Throws<DomainException>(() =>
            _service.Assign(diploma, CreateSession(), Guid.NewGuid(), [], hasApprovedTopic: false));
    }

    [Fact]
    public void Assign_WhenLifecycleNotTopicApproved_Throws()
    {
        Diploma diploma = CreateTopicApprovedDiploma();
        diploma.LifecycleStatus = DiplomaLifecycleStatus.ReviewerAssigned;
        diploma.ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned;

        Assert.Throws<DomainException>(() =>
            _service.Assign(diploma, CreateSession(), Guid.NewGuid(), [], hasApprovedTopic: true));
    }

    [Fact]
    public void Assign_WhenAdmissionAlreadyStarted_Throws()
    {
        Diploma diploma = CreateTopicApprovedDiploma();
        diploma.CurrentAdmissionStep = AdmissionStep.SupervisorFeedback;

        Assert.Throws<DomainException>(() =>
            _service.Assign(diploma, CreateSession(), Guid.NewGuid(), [], hasApprovedTopic: true));
    }

    [Fact]
    public void Assign_WhenAlreadyAssigned_Throws()
    {
        Diploma diploma = CreateTopicApprovedDiploma();
        diploma.ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned;
        diploma.ReviewerId = Guid.NewGuid();

        Assert.Throws<DomainException>(() =>
            _service.Assign(diploma, CreateSession(), Guid.NewGuid(), [], hasApprovedTopic: true));
    }

    [Fact]
    public void Assign_WhenCompleted_Throws()
    {
        Diploma diploma = CreateTopicApprovedDiploma();
        diploma.ReviewAssignmentStatus = ReviewAssignmentStatus.Completed;

        Assert.Throws<DomainException>(() =>
            _service.Assign(diploma, CreateSession(), Guid.NewGuid(), [], hasApprovedTopic: true));
    }

    private static Diploma CreateTopicApprovedDiploma() =>
        new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.TopicApproved,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
            CurrentAdmissionStep = null,
        };

    private static DefenceSession CreateSession() => new()
    {
        Status = DefenceSessionStatus.Active,
    };
}

public sealed class DiplomaAdmissionServiceTests
{
    private readonly DiplomaAdmissionService _service = new();

    [Fact]
    public void Admit_WhenReady_SetsAdmitted()
    {
        Diploma diploma = new()
        {
            AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
        };

        DateOnly defenceDate = new(2026, 6, 15);
        _service.Admit(diploma, CreateSession(), defenceDate, DiplomaLifecycleStatus.ReadyForAdmission);

        Assert.Equal(DiplomaAdmissionStatus.Admitted, diploma.AdmissionStatus);
        Assert.Equal(defenceDate, diploma.DefenceDate);
    }

    [Fact]
    public void Admit_WhenNotReady_Throws()
    {
        Diploma diploma = new();

        Assert.Throws<DomainException>(() =>
            _service.Admit(
                diploma,
                CreateSession(),
                new DateOnly(2026, 6, 15),
                DiplomaLifecycleStatus.DocumentsInProgress));
    }

    [Fact]
    public void Admit_WhenSessionArchived_Throws()
    {
        Diploma diploma = new()
        {
            AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
        };

        DefenceSession session = CreateSession();
        session.Status = DefenceSessionStatus.Archived;

        Assert.Throws<DomainException>(() =>
            _service.Admit(
                diploma,
                session,
                new DateOnly(2026, 6, 15),
                DiplomaLifecycleStatus.ReadyForAdmission));
    }

    [Fact]
    public void Admit_WhenAlreadyAdmitted_Throws()
    {
        Diploma diploma = new()
        {
            AdmissionStatus = DiplomaAdmissionStatus.Admitted,
        };

        Assert.Throws<DomainException>(() =>
            _service.Admit(
                diploma,
                CreateSession(),
                new DateOnly(2026, 6, 15),
                DiplomaLifecycleStatus.ReadyForAdmission));
    }

    private static DefenceSession CreateSession() => new()
    {
        Status = DefenceSessionStatus.Active,
    };
}
