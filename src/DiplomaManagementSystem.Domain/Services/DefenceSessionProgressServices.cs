using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Domain.Services;

public sealed class DefenceSessionMilestoneService
{
    public const int RequiredMilestoneCount = 3;

    public void ValidateMilestones(IReadOnlyList<(DateOnly DueDate, int ExpectedPercent)> milestones)
    {
        ArgumentNullException.ThrowIfNull(milestones);

        if (milestones.Count != RequiredMilestoneCount)
        {
            throw new DomainException("Exactly three milestones are required.");
        }

        for (int index = 0; index < milestones.Count; index++)
        {
            (DateOnly dueDate, int expectedPercent) = milestones[index];

            if (expectedPercent is < 0 or > 100)
            {
                throw new DomainException("Expected progress percent must be between 0 and 100.");
            }

            if (index > 0 && dueDate <= milestones[index - 1].DueDate)
            {
                throw new DomainException("Milestone dates must be unique and strictly increasing.");
            }
        }
    }

    public void EnsureSessionActive(DefenceSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        if (session.Status != DefenceSessionStatus.Active)
        {
            throw new DomainException("Defence session is not active.");
        }
    }

    public void ValidateActualPercent(int actualPercent)
    {
        if (actualPercent is < 0 or > 100)
        {
            throw new DomainException("Actual progress percent must be between 0 and 100.");
        }
    }
}

public sealed class DefenceDatePreferenceService
{
    public void EnsureCanRequest(
        Diploma diploma,
        DefenceSession session,
        DefenceDateOption option,
        bool preferenceAlreadyExists)
    {
        ArgumentNullException.ThrowIfNull(diploma);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(option);

        if (session.Status != DefenceSessionStatus.Active)
        {
            throw new DomainException("Defence session is not active.");
        }

        if (diploma.AdmissionStatus != DiplomaAdmissionStatus.Admitted)
        {
            throw new DomainException("Diploma must be admitted before requesting a defence date.");
        }

        if (preferenceAlreadyExists)
        {
            throw new DomainException("A defence date preference already exists for this diploma.");
        }

        if (option.DefenceSessionId != diploma.DefenceSessionId)
        {
            throw new DomainException("Selected defence date does not belong to this session.");
        }
    }

    public void EnsureCanRemoveDateOption(
        DefenceDateOption option,
        bool hasPreferences,
        bool isAssignedAsFinalDate)
    {
        ArgumentNullException.ThrowIfNull(option);

        if (hasPreferences || isAssignedAsFinalDate)
        {
            throw new DomainException("Defence date is in use and cannot be removed.");
        }
    }
}
