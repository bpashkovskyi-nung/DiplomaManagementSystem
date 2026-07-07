using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Domain;

public static class EmployeeWorkloadLimitPolicy
{
    public static void EnsureCanTakeSupervisorSlot(int currentCountExcludingDiploma, int? maxLimit)
    {
        if (!maxLimit.HasValue)
        {
            return;
        }

        if (currentCountExcludingDiploma + 1 > maxLimit.Value)
        {
            throw new DomainException(EmployeeWorkloadLimitMessages.SupervisorLimitReached);
        }
    }

    public static void EnsureCanTakeReviewerSlot(int currentCountExcludingDiploma, int? maxLimit)
    {
        if (!maxLimit.HasValue)
        {
            return;
        }

        if (currentCountExcludingDiploma + 1 > maxLimit.Value)
        {
            throw new DomainException(EmployeeWorkloadLimitMessages.ReviewerLimitReached);
        }
    }
}
