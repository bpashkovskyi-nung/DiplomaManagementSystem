using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class DiplomaWorkflowInvariantValidatorTests
{
    private readonly DiplomaWorkflowInvariantValidator _validator = new();

    [Fact]
    public void ValidateTopicVersion_ApprovedWithConfirmedSupervisor_DoesNotThrow()
    {
        var supervisorId = Guid.NewGuid();
        Diploma diploma = CreateDiploma(supervisorId, SupervisorAssignmentStatus.Confirmed, DiplomaLifecycleStatus.ReviewerAssigned);
        DiplomaTopicVersion version = CreateTopicVersion(TopicVersionStatus.Approved, supervisorId);

        _validator.ValidateTopicVersion(diploma, version);
    }

    [Fact]
    public void ValidateTopicVersion_ApprovedWithoutSupervisor_Throws()
    {
        Diploma diploma = CreateDiploma(supervisorId: null, SupervisorAssignmentStatus.Pending, DiplomaLifecycleStatus.ReviewerAssigned);
        DiplomaTopicVersion version = CreateTopicVersion(TopicVersionStatus.Approved, supervisorId: Guid.NewGuid());

        DomainException exception = Assert.Throws<DomainException>(() =>
            _validator.ValidateTopicVersion(diploma, version));

        Assert.Contains("confirmed supervisor", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTopicVersion_ApprovedWithoutSupervisorReview_Throws()
    {
        var supervisorId = Guid.NewGuid();
        Diploma diploma = CreateDiploma(supervisorId, SupervisorAssignmentStatus.Confirmed, DiplomaLifecycleStatus.ReviewerAssigned);
        DiplomaTopicVersion version = CreateTopicVersion(TopicVersionStatus.Approved, supervisorId: null);

        DomainException exception = Assert.Throws<DomainException>(() =>
            _validator.ValidateTopicVersion(diploma, version));

        Assert.Contains("supervisor review metadata", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateTopicVersion_PendingHeadWithMismatchedSupervisor_Throws()
    {
        var supervisorId = Guid.NewGuid();
        Diploma diploma = CreateDiploma(supervisorId, SupervisorAssignmentStatus.Confirmed, DiplomaLifecycleStatus.TopicInReview);
        DiplomaTopicVersion version = CreateTopicVersion(TopicVersionStatus.PendingHead, supervisorId: Guid.NewGuid());

        DomainException exception = Assert.Throws<DomainException>(() =>
            _validator.ValidateTopicVersion(diploma, version));

        Assert.Contains("does not match", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateSupervisorRequiredForLifecycle_TopicInReviewWithoutConfirmedSupervisor_Throws()
    {
        Diploma diploma = CreateDiploma(supervisorId: null, SupervisorAssignmentStatus.Pending, DiplomaLifecycleStatus.TopicInReview);

        DomainException exception = Assert.Throws<DomainException>(() =>
            _validator.ValidateSupervisorRequiredForLifecycle(diploma));

        Assert.Contains("confirmed supervisor", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_RejectsAggregateWithInvalidApprovedTopic()
    {
        Diploma diploma = CreateDiploma(supervisorId: null, SupervisorAssignmentStatus.Pending, DiplomaLifecycleStatus.ReviewerAssigned);
        DiplomaTopicVersion version = CreateTopicVersion(TopicVersionStatus.Approved, supervisorId: Guid.NewGuid());

        Assert.Throws<DomainException>(() => _validator.Validate(diploma, [version]));
    }

    private static Diploma CreateDiploma(
        Guid? supervisorId,
        SupervisorAssignmentStatus supervisorAssignmentStatus,
        DiplomaLifecycleStatus lifecycleStatus) =>
        new()
        {
            Id = Guid.NewGuid(),
            SupervisorId = supervisorId,
            SupervisorAssignmentStatus = supervisorAssignmentStatus,
            LifecycleStatus = lifecycleStatus,
        };

    private static DiplomaTopicVersion CreateTopicVersion(TopicVersionStatus status, Guid? supervisorId) =>
        new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = Guid.NewGuid(),
            VersionNumber = 1,
            Title = "Topic",
            Status = status,
            SubmittedAt = DateTimeOffset.UtcNow,
            SupervisorReviewedById = supervisorId,
            SupervisorReviewedAt = supervisorId is null ? null : DateTimeOffset.UtcNow,
        };
}
