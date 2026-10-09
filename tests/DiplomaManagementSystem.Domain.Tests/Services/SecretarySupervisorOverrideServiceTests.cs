using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class SecretarySupervisorOverrideServiceTests
{
    private readonly SecretarySupervisorOverrideService _service = new();

    // TC-DOM-SSO-001
    [Fact]
    public void Override_SetsSupervisorAndConfirmed()
    {
        var newSupervisorId = Guid.NewGuid();
        Diploma diploma = CreateWritableDiploma();
        DefenceSession session = CreateActiveSession();
        DateTimeOffset before = DateTimeOffset.UtcNow;

        _service.Override(diploma, session, newSupervisorId);

        Assert.Equal(newSupervisorId, diploma.SupervisorId);
        Assert.Equal(SupervisorAssignmentStatus.Confirmed, diploma.SupervisorAssignmentStatus);
        Assert.True(diploma.UpdatedAt >= before);
    }

    // TC-DOM-SSO-002
    [Fact]
    public void Override_WhenAdmitted_Throws()
    {
        Diploma diploma = CreateWritableDiploma();
        diploma.AdmissionStatus = DiplomaAdmissionStatus.Admitted;
        DefenceSession session = CreateActiveSession();

        Assert.Throws<DomainException>(() => _service.Override(diploma, session, Guid.NewGuid()));
    }

    private static Diploma CreateWritableDiploma() => new()
    {
        LifecycleStatus = DiplomaLifecycleStatus.AwaitingSupervisor,
        AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
        SupervisorAssignmentStatus = SupervisorAssignmentStatus.Pending,
    };

    private static DefenceSession CreateActiveSession() => new()
    {
        Status = DefenceSessionStatus.Active,
    };
}
