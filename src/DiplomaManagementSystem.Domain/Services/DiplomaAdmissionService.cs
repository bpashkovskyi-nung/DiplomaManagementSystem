using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Domain.Services;

public sealed class DiplomaAdmissionService
{
    public void Admit(
        Diploma diploma,
        DefenceSession defenceSession,
        DiplomaLifecycleStatus currentLifecycleStatus)
    {
        ArgumentNullException.ThrowIfNull(diploma);
        ArgumentNullException.ThrowIfNull(defenceSession);

        if (defenceSession.Status == DefenceSessionStatus.Archived)
        {
            throw new DomainException("Defence session is archived.");
        }

        if (diploma.AdmissionStatus == DiplomaAdmissionStatus.Admitted)
        {
            throw new DomainException("Diploma is already admitted.");
        }

        if (currentLifecycleStatus != DiplomaLifecycleStatus.ReadyForAdmission)
        {
            throw new DomainException("Diploma is not ready for admission.");
        }

        diploma.AdmissionStatus = DiplomaAdmissionStatus.Admitted;
        diploma.UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ConfirmDefenceDate(
        Diploma diploma,
        DefenceSession defenceSession,
        DateOnly defenceDate,
        IReadOnlyCollection<DefenceDateOption> availableDates)
    {
        ArgumentNullException.ThrowIfNull(diploma);
        ArgumentNullException.ThrowIfNull(defenceSession);
        ArgumentNullException.ThrowIfNull(availableDates);

        if (defenceSession.Status == DefenceSessionStatus.Archived)
        {
            throw new DomainException("Defence session is archived.");
        }

        if (diploma.AdmissionStatus != DiplomaAdmissionStatus.Admitted)
        {
            throw new DomainException("Diploma must be admitted before confirming a defence date.");
        }

        if (!availableDates.Any(option => option.Date == defenceDate))
        {
            throw new DomainException("Selected defence date is not available for this session.");
        }

        diploma.DefenceDate = defenceDate;
        diploma.UpdatedAt = DateTimeOffset.UtcNow;
    }
}
