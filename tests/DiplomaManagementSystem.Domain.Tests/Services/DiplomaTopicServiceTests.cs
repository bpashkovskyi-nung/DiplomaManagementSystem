using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class DiplomaTopicServiceTests
{
    private readonly DiplomaTopicService _service = new();

    [Fact]
    public void CanSubmitNewVersion_WhenPendingHead_ReturnsFalse()
    {
        List<DiplomaTopicVersion> versions =
        [
            new() { Status = TopicVersionStatus.PendingHead },
        ];

        bool result = _service.CanSubmitNewVersion(versions);

        Assert.False(result);
    }

    [Fact]
    public void EnsureCanSubmitNewVersion_WhenPendingHead_Throws()
    {
        List<DiplomaTopicVersion> versions =
        [
            new() { Status = TopicVersionStatus.PendingHead },
        ];

        Assert.Throws<DomainException>(() => _service.EnsureCanSubmitNewVersion(versions));
    }

    [Fact]
    public void CreateVersion_TrimsTitle()
    {
        var diplomaId = Guid.NewGuid();
        DiplomaTopicVersion version = _service.CreateVersion(diplomaId, "  Тема роботи  ", 1);

        Assert.Equal("Тема роботи", version.Title);
        Assert.Equal(TopicVersionStatus.PendingSupervisor, version.Status);
        Assert.Equal(diplomaId, version.DiplomaId);
    }

    [Fact]
    public void CreateVersion_WhenTitleEmpty_Throws()
    {
        Assert.Throws<DomainException>(() =>
            _service.CreateVersion(Guid.NewGuid(), "   ", 1));
    }

    [Fact]
    public void GetNextVersionNumber_WhenEmpty_ReturnsOne()
    {
        int next = _service.GetNextVersionNumber([]);

        Assert.Equal(1, next);
    }

    [Fact]
    public void GetNextVersionNumber_WhenVersionsExist_ReturnsMaxPlusOne()
    {
        List<DiplomaTopicVersion> versions =
        [
            new() { VersionNumber = 1 },
            new() { VersionNumber = 3 },
        ];

        int next = _service.GetNextVersionNumber(versions);

        Assert.Equal(4, next);
    }

    [Fact]
    public void CanSubmitNewVersion_WhenNoApproved_ReturnsTrue()
    {
        List<DiplomaTopicVersion> versions =
        [
            new() { Status = TopicVersionStatus.Rejected },
        ];

        bool result = _service.CanSubmitNewVersion(versions);

        Assert.True(result);
    }

    [Fact]
    public void CanSubmitNewVersion_WhenApprovedExists_ReturnsFalse()
    {
        List<DiplomaTopicVersion> versions =
        [
            new() { Status = TopicVersionStatus.Approved },
        ];

        bool result = _service.CanSubmitNewVersion(versions);

        Assert.False(result);
    }

    [Fact]
    public void CanSubmitNewVersion_WhenPendingSupervisor_ReturnsFalse()
    {
        List<DiplomaTopicVersion> versions =
        [
            new() { Status = TopicVersionStatus.PendingSupervisor },
        ];

        bool result = _service.CanSubmitNewVersion(versions);

        Assert.False(result);
    }

    [Fact]
    public void EnsureCanSubmitNewVersion_WhenApproved_Throws()
    {
        List<DiplomaTopicVersion> versions =
        [
            new() { Status = TopicVersionStatus.Approved },
        ];

        Assert.Throws<DomainException>(() => _service.EnsureCanSubmitNewVersion(versions));
    }
}
