using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain.Services;

public sealed class DiplomaLifecycleService(
    AdmissionReadinessService admissionReadinessService,
    DiplomaWorkflowInvariantValidator validator)
{
    private readonly DiplomaWorkflowInvariantValidator _validator = validator;

    public DiplomaLifecycleService(AdmissionReadinessService admissionReadinessService)
        : this(admissionReadinessService, new DiplomaWorkflowInvariantValidator())
    {
    }

    public DiplomaLifecycleStatus Recalculate(
        Diploma diploma,
        DiplomaTopicVersion? latestTopicVersion,
        IEnumerable<DiplomaAdmissionStepAttempt> attempts)
    {
        ArgumentNullException.ThrowIfNull(diploma);
        ArgumentNullException.ThrowIfNull(attempts);

        if (diploma.AdmissionStatus == DiplomaAdmissionStatus.Admitted)
        {
            return DiplomaLifecycleStatus.Admitted;
        }

        if (admissionReadinessService.IsReadyForAdmission(diploma, latestTopicVersion, attempts))
        {
            return DiplomaLifecycleStatus.ReadyForAdmission;
        }

        List<DiplomaAdmissionStepAttempt> attemptList = attempts.ToList();

        if (attemptList.Count > 0 || diploma.CurrentAdmissionStep is not null)
        {
            return DiplomaLifecycleStatus.DocumentsInProgress;
        }

        if (latestTopicVersion?.Status == TopicVersionStatus.Approved)
        {
            _validator.ValidateTopicVersion(diploma, latestTopicVersion);

            if (diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.Assigned
                || diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.Completed
                || diploma.ReviewerId is not null)
            {
                return DiplomaLifecycleStatus.ReviewerAssigned;
            }

            return DiplomaLifecycleStatus.TopicApproved;
        }

        if (latestTopicVersion is not null)
        {
            _validator.ValidateTopicVersion(diploma, latestTopicVersion);
            return DiplomaLifecycleStatus.TopicInReview;
        }

        if (diploma.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Confirmed)
        {
            return DiplomaLifecycleStatus.SupervisorConfirmed;
        }

        return DiplomaLifecycleStatus.AwaitingSupervisor;
    }

    public bool CanStartAdmissionReview(
        DiplomaLifecycleStatus currentStatus,
        DiplomaTopicVersion? latestTopicVersion,
        int attemptCount,
        AdmissionStep? currentAdmissionStep) =>
        latestTopicVersion?.Status == TopicVersionStatus.Approved
        && currentStatus == DiplomaLifecycleStatus.ReviewerAssigned
        && attemptCount == 0
        && currentAdmissionStep is null;
}
