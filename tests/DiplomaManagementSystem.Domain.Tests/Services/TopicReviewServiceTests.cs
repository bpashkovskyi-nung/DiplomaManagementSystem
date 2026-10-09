using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class TopicReviewServiceTests
{
    private readonly TopicReviewService _service = new();

    [Fact]
    public void SupervisorReject_WhenValid_SetsRejected()
    {
        var reviewerId = Guid.NewGuid();
        DiplomaTopicVersion version = CreateVersion(TopicVersionStatus.PendingSupervisor);

        _service.SupervisorReject(version, reviewerId, "  Потрібні правки  ");

        Assert.Equal(TopicVersionStatus.Rejected, version.Status);
        Assert.Equal("Потрібні правки", version.RejectionReason);
        Assert.Equal(reviewerId, version.ReviewedById);
        Assert.NotNull(version.ReviewedAt);
    }

    [Fact]
    public void DepartmentHeadReject_WhenValid_SetsRejected()
    {
        var headId = Guid.NewGuid();
        DiplomaTopicVersion version = CreateVersion(TopicVersionStatus.PendingHead);

        _service.DepartmentHeadReject(version, headId, "Не відповідає вимогам");

        Assert.Equal(TopicVersionStatus.Rejected, version.Status);
        Assert.Equal("Не відповідає вимогам", version.RejectionReason);
        Assert.Equal(headId, version.ReviewedById);
    }

    [Fact]
    public void DepartmentHeadReject_WithoutReason_Throws()
    {
        DiplomaTopicVersion version = CreateVersion(TopicVersionStatus.PendingHead);

        Assert.Throws<DomainException>(() =>
            _service.DepartmentHeadReject(version, Guid.NewGuid(), " "));
    }

    [Fact]
    public void SupervisorApprove_WhenWrongStatus_Throws()
    {
        DiplomaTopicVersion version = CreateVersion(TopicVersionStatus.PendingHead);

        Assert.Throws<DomainException>(() =>
            _service.SupervisorApprove(version, Guid.NewGuid()));
    }

    [Fact]
    public void SupervisorApprove_MovesToPendingHead()
    {
        var reviewerId = Guid.NewGuid();
        DiplomaTopicVersion version = CreateVersion(TopicVersionStatus.PendingSupervisor);

        _service.SupervisorApprove(version, reviewerId);

        Assert.Equal(TopicVersionStatus.PendingHead, version.Status);
        Assert.Equal(reviewerId, version.SupervisorReviewedById);
        Assert.NotNull(version.SupervisorReviewedAt);
        Assert.Null(version.ReviewedById);
        Assert.Null(version.ReviewedAt);
    }

    [Fact]
    public void DepartmentHeadApprove_MovesToApproved()
    {
        var supervisorId = Guid.NewGuid();
        var headId = Guid.NewGuid();
        DiplomaTopicVersion version = CreateVersion(TopicVersionStatus.PendingHead);
        version.SupervisorReviewedById = supervisorId;
        version.SupervisorReviewedAt = DateTimeOffset.UtcNow.AddHours(-1);

        _service.DepartmentHeadApprove(version, headId);

        Assert.Equal(TopicVersionStatus.Approved, version.Status);
        Assert.Equal(headId, version.ReviewedById);
        Assert.Equal(supervisorId, version.SupervisorReviewedById);
    }

    [Fact]
    public void SupervisorReject_WithoutReason_Throws()
    {
        DiplomaTopicVersion version = CreateVersion(TopicVersionStatus.PendingSupervisor);

        Assert.Throws<DomainException>(() =>
            _service.SupervisorReject(version, Guid.NewGuid(), " "));
    }

    private static DiplomaTopicVersion CreateVersion(TopicVersionStatus status) => new()
    {
        Id = Guid.NewGuid(),
        Status = status,
        Title = "Topic",
    };
}
