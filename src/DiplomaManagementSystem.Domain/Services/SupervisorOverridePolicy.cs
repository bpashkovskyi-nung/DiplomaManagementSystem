using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Domain.Services;

public static class SupervisorOverridePolicy
{
    public static bool AllowsLifecycleOverride(DiplomaLifecycleStatus lifecycleStatus) =>
        lifecycleStatus < DiplomaLifecycleStatus.WorkInProgressByStudent;

    public static bool AllowsAdmissionOverride(DiplomaAdmissionStatus admissionStatus) =>
        admissionStatus != DiplomaAdmissionStatus.Admitted;

    public static void EnsureCanOverride(Diploma diploma, DefenceSession defenceSession)
    {
        ArgumentNullException.ThrowIfNull(diploma);
        ArgumentNullException.ThrowIfNull(defenceSession);

        if (defenceSession.Status == DefenceSessionStatus.Archived)
        {
            throw new DomainException(SupervisorOverridePolicyMessages.SessionArchived);
        }

        if (!AllowsAdmissionOverride(diploma.AdmissionStatus))
        {
            throw new DomainException(SupervisorOverridePolicyMessages.AlreadyAdmitted);
        }

        if (!AllowsLifecycleOverride(diploma.LifecycleStatus))
        {
            throw new DomainException(SupervisorOverridePolicyMessages.TopicAlreadyApproved);
        }
    }
}

internal static class SupervisorOverridePolicyMessages
{
    internal const string SessionArchived = "Сесія заархівована — зміни недоступні.";

    internal const string AlreadyAdmitted = "Неможливо змінити керівника для допущеної роботи.";

    internal const string TopicAlreadyApproved = "Після затвердження теми змінити керівника не можна.";
}
