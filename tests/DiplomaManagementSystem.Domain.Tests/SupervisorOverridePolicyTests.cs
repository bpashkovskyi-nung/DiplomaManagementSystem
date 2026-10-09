using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests;

public sealed class SupervisorOverridePolicyTests
{
    // TC-DOM-SOP-001
    [Fact]
    public void AllowsLifecycle_BeforeWorkInProgress_ReturnsTrue()
    {
        Assert.True(SupervisorOverridePolicy.AllowsLifecycleOverride(DiplomaLifecycleStatus.AwaitingSupervisor));
    }

    // TC-DOM-SOP-002
    [Fact]
    public void AllowsLifecycle_AfterTopicApproved_ReturnsFalse()
    {
        Assert.False(SupervisorOverridePolicy.AllowsLifecycleOverride(DiplomaLifecycleStatus.WorkInProgressByStudent));
    }

    // TC-DOM-SOP-003
    [Fact]
    public void AllowsAdmission_NotAdmitted_ReturnsTrue()
    {
        Assert.True(SupervisorOverridePolicy.AllowsAdmissionOverride(DiplomaAdmissionStatus.NotAdmitted));
    }

    // TC-DOM-SOP-004
    [Fact]
    public void AllowsAdmission_Admitted_ReturnsFalse()
    {
        Assert.False(SupervisorOverridePolicy.AllowsAdmissionOverride(DiplomaAdmissionStatus.Admitted));
    }

    // TC-DOM-SOP-005
    [Fact]
    public void EnsureCanOverride_ArchivedSession_Throws()
    {
        Diploma diploma = CreateDiploma();
        DefenceSession session = CreateSession(DefenceSessionStatus.Archived);

        DomainException exception = Assert.Throws<DomainException>(() =>
            SupervisorOverridePolicy.EnsureCanOverride(diploma, session));

        Assert.Contains("архів", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // TC-DOM-SOP-006
    [Fact]
    public void EnsureCanOverride_AlreadyAdmitted_Throws()
    {
        Diploma diploma = CreateDiploma();
        diploma.AdmissionStatus = DiplomaAdmissionStatus.Admitted;
        DefenceSession session = CreateSession(DefenceSessionStatus.Active);

        DomainException exception = Assert.Throws<DomainException>(() =>
            SupervisorOverridePolicy.EnsureCanOverride(diploma, session));

        Assert.Contains("допущен", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // TC-DOM-SOP-007
    [Fact]
    public void EnsureCanOverride_TopicApprovedLifecycle_Throws()
    {
        Diploma diploma = CreateDiploma();
        diploma.LifecycleStatus = DiplomaLifecycleStatus.WorkInProgressByStudent;
        DefenceSession session = CreateSession(DefenceSessionStatus.Active);

        DomainException exception = Assert.Throws<DomainException>(() =>
            SupervisorOverridePolicy.EnsureCanOverride(diploma, session));

        Assert.Contains("тем", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // TC-DOM-SOP-008
    [Fact]
    public void EnsureCanOverride_Valid_DoesNotThrow()
    {
        Diploma diploma = CreateDiploma();
        DefenceSession session = CreateSession(DefenceSessionStatus.Active);

        Exception? exception = Record.Exception(() => SupervisorOverridePolicy.EnsureCanOverride(diploma, session));

        Assert.Null(exception);
    }

    private static Diploma CreateDiploma() => new()
    {
        LifecycleStatus = DiplomaLifecycleStatus.AwaitingSupervisor,
        AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
    };

    private static DefenceSession CreateSession(DefenceSessionStatus status) => new()
    {
        Status = status,
    };
}
