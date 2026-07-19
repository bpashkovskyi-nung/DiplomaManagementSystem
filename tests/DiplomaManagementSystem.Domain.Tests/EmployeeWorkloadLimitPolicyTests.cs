using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Domain.Tests;

public sealed class EmployeeWorkloadLimitPolicyTests
{
    [Fact]
    public void EnsureCanTakeSupervisorSlot_WhenBelowLimit_DoesNotThrow()
    {
        EmployeeWorkloadLimitPolicy.EnsureCanTakeSupervisorSlot(2, 3);
    }

    [Fact]
    public void EnsureCanTakeSupervisorSlot_WhenAtLimit_Throws()
    {
        DomainException exception = Assert.Throws<DomainException>(() =>
            EmployeeWorkloadLimitPolicy.EnsureCanTakeSupervisorSlot(3, 3));

        Assert.Equal(EmployeeWorkloadLimitMessages.SupervisorLimitReached, exception.Message);
    }

    [Fact]
    public void EnsureCanTakeSupervisorSlot_WhenNoLimit_DoesNotThrow()
    {
        EmployeeWorkloadLimitPolicy.EnsureCanTakeSupervisorSlot(100, null);
    }

    [Fact]
    public void EnsureCanTakeReviewerSlot_WhenAtLimit_Throws()
    {
        DomainException exception = Assert.Throws<DomainException>(() =>
            EmployeeWorkloadLimitPolicy.EnsureCanTakeReviewerSlot(1, 1));

        Assert.Equal(EmployeeWorkloadLimitMessages.ReviewerLimitReached, exception.Message);
    }

    [Fact]
    public void EnsureCanTakeReviewerSlot_WhenBelowLimit_DoesNotThrow()
    {
        EmployeeWorkloadLimitPolicy.EnsureCanTakeReviewerSlot(0, 1);
    }

    [Fact]
    public void EnsureCanTakeSupervisorSlot_WhenExactlyOneSlotLeft_DoesNotThrow()
    {
        EmployeeWorkloadLimitPolicy.EnsureCanTakeSupervisorSlot(1, 2);
    }
}
