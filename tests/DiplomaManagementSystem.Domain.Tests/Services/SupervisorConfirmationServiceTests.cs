using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class SupervisorConfirmationServiceTests
{
    private readonly SupervisorConfirmationService _service = new();

    [Fact]
    public void Confirm_WhenPending_SetsConfirmed()
    {
        var supervisorId = Guid.NewGuid();
        Diploma diploma = CreateDiploma(supervisorId, SupervisorAssignmentStatus.Pending);
        DefenceSession session = CreateSession();

        _service.Confirm(diploma, session, supervisorId);

        Assert.Equal(SupervisorAssignmentStatus.Confirmed, diploma.SupervisorAssignmentStatus);
        Assert.NotEqual(default, diploma.UpdatedAt);
    }

    [Fact]
    public void Reject_WhenPending_SetsRejected()
    {
        var supervisorId = Guid.NewGuid();
        Diploma diploma = CreateDiploma(supervisorId, SupervisorAssignmentStatus.Pending);
        DefenceSession session = CreateSession();

        _service.Reject(diploma, session, supervisorId);

        Assert.Equal(SupervisorAssignmentStatus.Rejected, diploma.SupervisorAssignmentStatus);
        Assert.NotEqual(default, diploma.UpdatedAt);
    }

    [Fact]
    public void Confirm_WhenNotSupervisor_Throws()
    {
        Diploma diploma = CreateDiploma(Guid.NewGuid(), SupervisorAssignmentStatus.Pending);
        DefenceSession session = CreateSession();

        Assert.Throws<DomainException>(() =>
            _service.Confirm(diploma, session, Guid.NewGuid()));
    }

    [Fact]
    public void Confirm_WhenNotPending_Throws()
    {
        var supervisorId = Guid.NewGuid();
        Diploma diploma = CreateDiploma(supervisorId, SupervisorAssignmentStatus.Confirmed);
        DefenceSession session = CreateSession();

        Assert.Throws<DomainException>(() =>
            _service.Confirm(diploma, session, supervisorId));
    }

    [Fact]
    public void Confirm_WhenSessionArchived_Throws()
    {
        var supervisorId = Guid.NewGuid();
        Diploma diploma = CreateDiploma(supervisorId, SupervisorAssignmentStatus.Pending);
        DefenceSession session = CreateSession();
        session.Status = DefenceSessionStatus.Archived;

        Assert.Throws<DomainException>(() =>
            _service.Confirm(diploma, session, supervisorId));
    }

    [Fact]
    public void Reject_WhenNotSupervisor_Throws()
    {
        Diploma diploma = CreateDiploma(Guid.NewGuid(), SupervisorAssignmentStatus.Pending);
        DefenceSession session = CreateSession();

        Assert.Throws<DomainException>(() =>
            _service.Reject(diploma, session, Guid.NewGuid()));
    }

    private static Diploma CreateDiploma(Guid supervisorId, SupervisorAssignmentStatus status) => new()
    {
        Id = Guid.NewGuid(),
        SupervisorId = supervisorId,
        SupervisorAssignmentStatus = status,
    };

    private static DefenceSession CreateSession() => new()
    {
        Id = Guid.NewGuid(),
        Status = DefenceSessionStatus.Active,
    };
}
