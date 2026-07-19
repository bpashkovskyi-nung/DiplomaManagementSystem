using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class SupervisorSelectionServiceTests
{
    private readonly SupervisorSelectionService _service = new();

    [Fact]
    public void RequestSupervisor_WhenAlreadyConfirmed_Throws()
    {
        Diploma diploma = CreateDiploma();
        diploma.SupervisorId = Guid.NewGuid();
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed;
        DefenceSession session = CreateSession();

        Assert.Throws<DomainException>(() =>
            _service.RequestSupervisor(diploma, session, Guid.NewGuid()));
    }

    [Fact]
    public void RequestSupervisor_WhenValid_SetsPending()
    {
        Diploma diploma = CreateDiploma();
        DefenceSession session = CreateSession();
        var supervisorId = Guid.NewGuid();

        _service.RequestSupervisor(diploma, session, supervisorId);

        Assert.Equal(supervisorId, diploma.SupervisorId);
        Assert.Equal(SupervisorAssignmentStatus.Pending, diploma.SupervisorAssignmentStatus);
    }

    [Fact]
    public void RequestSupervisor_WhenAlreadyPending_Throws()
    {
        Diploma diploma = CreateDiploma();
        diploma.SupervisorId = Guid.NewGuid();
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Pending;
        DefenceSession session = CreateSession();
        var supervisorId = Guid.NewGuid();

        Assert.Throws<DomainException>(() =>
            _service.RequestSupervisor(diploma, session, supervisorId));
    }

    [Fact]
    public void RequestSupervisor_WhenPendingWithoutSupervisor_AllowsSelection()
    {
        Diploma diploma = CreateDiploma();
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Pending;
        DefenceSession session = CreateSession();
        var supervisorId = Guid.NewGuid();

        _service.RequestSupervisor(diploma, session, supervisorId);

        Assert.Equal(supervisorId, diploma.SupervisorId);
        Assert.Equal(SupervisorAssignmentStatus.Pending, diploma.SupervisorAssignmentStatus);
    }

    [Fact]
    public void RequestSupervisor_WhenSessionArchived_Throws()
    {
        Diploma diploma = CreateDiploma();
        DefenceSession session = CreateSession();
        session.Status = DefenceSessionStatus.Archived;
        var supervisorId = Guid.NewGuid();

        Assert.Throws<DomainException>(() =>
            _service.RequestSupervisor(diploma, session, supervisorId));
    }

    private static Diploma CreateDiploma() => new()
    {
        Id = Guid.NewGuid(),
        StudentId = Guid.NewGuid(),
        SupervisorAssignmentStatus = SupervisorAssignmentStatus.Rejected,
        LifecycleStatus = DiplomaLifecycleStatus.AwaitingSupervisor,
    };

    private static DefenceSession CreateSession() => new()
    {
        Id = Guid.NewGuid(),
        Status = DefenceSessionStatus.Active,
    };
}
