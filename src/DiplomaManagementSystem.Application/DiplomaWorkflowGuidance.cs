using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Application;

public static class DiplomaWorkflowGuidance
{
    public static string? BuildAssignReviewerBlockedReason(
        bool showSection,
        bool hasApprovedTopic,
        bool hasEmployees,
        IEnumerable<DiplomaTopicVersion> topicVersions,
        ReviewAssignmentStatus reviewAssignmentStatus)
    {
        if (!showSection)
        {
            return null;
        }

        if (!hasEmployees)
        {
            return WorkflowGuidanceMessages.NoEmployeesAdmin;
        }

        if (!hasApprovedTopic)
        {
            return BuildTopicApprovalBlockedReason(topicVersions);
        }

        if (reviewAssignmentStatus == ReviewAssignmentStatus.Assigned)
        {
            return WorkflowGuidanceMessages.ReviewerAlreadyAssigned;
        }

        if (reviewAssignmentStatus == ReviewAssignmentStatus.Completed)
        {
            return WorkflowGuidanceMessages.ReviewAlreadyCompleted;
        }

        return null;
    }

    public static string? BuildDeclareWorkReadyBlockedReason(
        bool showSection,
        bool sessionActive,
        DiplomaLifecycleStatus lifecycleStatus,
        bool hasStudentWork)
    {
        if (!showSection)
        {
            return null;
        }

        if (!sessionActive)
        {
            return WorkflowGuidanceMessages.SessionArchivedActions;
        }

        if (lifecycleStatus != DiplomaLifecycleStatus.WorkInProgressByStudent)
        {
            return WorkflowGuidanceMessages.ChecksAlreadyStarted;
        }

        if (!hasStudentWork)
        {
            return WorkflowGuidanceMessages.UploadWorkFirst;
        }

        return null;
    }

    public static string? BuildUploadWorkBlockedReason(
        bool showSection,
        bool sessionActive,
        bool hasApprovedTopic,
        DiplomaLifecycleStatus lifecycleStatus)
    {
        if (!showSection)
        {
            return null;
        }

        if (!sessionActive)
        {
            return WorkflowGuidanceMessages.SessionArchivedUpload;
        }

        if (!hasApprovedTopic)
        {
            return WorkflowGuidanceMessages.UploadAfterTopicApproved;
        }

        if (lifecycleStatus == DiplomaLifecycleStatus.Admitted)
        {
            return WorkflowGuidanceMessages.UploadAfterAdmitted;
        }

        if (lifecycleStatus is not (
            DiplomaLifecycleStatus.WorkInProgressByStudent
            or DiplomaLifecycleStatus.DocumentsInProgress
            or DiplomaLifecycleStatus.ReadyForAdmission
            or DiplomaLifecycleStatus.TopicApproved))
        {
            return WorkflowGuidanceMessages.UploadWrongLifecycle;
        }

        return null;
    }

    public static string? BuildOverrideSupervisorBlockedReason(
        bool showSection,
        bool sessionActive,
        bool hasEmployees,
        DiplomaLifecycleStatus lifecycleStatus)
    {
        if (!showSection)
        {
            return null;
        }

        if (!sessionActive)
        {
            return WorkflowGuidanceMessages.SessionArchivedShort;
        }

        if (!SupervisorOverridePolicy.AllowsLifecycleOverride(lifecycleStatus))
        {
            return WorkflowGuidanceMessages.SupervisorChangeAfterTopic;
        }

        if (!hasEmployees)
        {
            return WorkflowGuidanceMessages.NoEmployeesAdmin;
        }

        return null;
    }

    public static string? BuildAdmitBlockedReason(
        bool showSection,
        bool sessionActive,
        Diploma diploma,
        IEnumerable<DiplomaTopicVersion> topicVersions,
        IEnumerable<DiplomaAdmissionStepAttempt> attempts)
    {
        if (!showSection)
        {
            return null;
        }

        if (!sessionActive)
        {
            return WorkflowGuidanceMessages.SessionArchivedAdmit;
        }

        if (diploma.LifecycleStatus == DiplomaLifecycleStatus.ReadyForAdmission)
        {
            return null;
        }

        List<string> blockers = [];

        if (diploma.SupervisorAssignmentStatus != SupervisorAssignmentStatus.Confirmed)
        {
            blockers.Add(WorkflowGuidanceMessages.SupervisorNotConfirmed);
        }

        if (!topicVersions.Any(version => version.Status == TopicVersionStatus.Approved))
        {
            blockers.Add(WorkflowGuidanceMessages.TopicNotApproved);
        }

        if (!attempts.Any() && diploma.CurrentAdmissionStep is null)
        {
            blockers.Add(WorkflowGuidanceMessages.ChecksNotStarted);
        }
        else
        {
            foreach (AdmissionStep step in AdmissionStepSequence.OutcomeSteps)
            {
                if (!AdmissionStepStatusResolver.HasPassingAttempt(step, attempts))
                {
                    blockers.Add($"{WorkflowGuidanceMessages.AdmitStepIncompletePrefix}{WorkflowUkrainianLabels.FormatAdmissionStepInline(step)}");
                }
            }
        }

        if (diploma.ReviewAssignmentStatus != ReviewAssignmentStatus.Completed)
        {
            blockers.Add(WorkflowGuidanceMessages.ReviewNotCompleted);
        }

        if (blockers.Count == 0)
        {
            return WorkflowGuidanceMessages.AdmitConditionsUpdating;
        }

        return WorkflowGuidanceMessages.AdmitNotReadyPrefix + string.Join("; ", blockers) + ".";
    }

    public static string? BuildOverrideAdmissionStepBlockedReason(
        bool showSection,
        bool sessionActive,
        bool admissionReviewStarted,
        Diploma diploma,
        IEnumerable<DiplomaAdmissionStepAttempt> attempts)
    {
        if (!showSection)
        {
            return null;
        }

        if (!sessionActive)
        {
            return WorkflowUkrainianLabels.SecretaryOverrideArchivedBlocked;
        }

        if (!admissionReviewStarted || diploma.CurrentAdmissionStep is null)
        {
            return WorkflowGuidanceMessages.OverrideBeforeWorkReady;
        }

        if (!AdmissionStepStatusResolver.CanSecretaryOverrideCurrentStep(diploma, attempts))
        {
            if (!AdmissionStepSequence.AcceptsOutcome(diploma.CurrentAdmissionStep.Value))
            {
                return WorkflowGuidanceMessages.OverrideWrongStepType;
            }

            if (!AdmissionStepStatusResolver.IsStepActionable(
                    diploma.CurrentAdmissionStep.Value,
                    attempts))
            {
                return WorkflowGuidanceMessages.OverrideStepCompleted;
            }

            if (diploma.CurrentAdmissionStep == AdmissionStep.ExternalReview
                && diploma.ReviewAssignmentStatus != ReviewAssignmentStatus.Assigned)
            {
                return WorkflowGuidanceMessages.OverrideReviewerNotAssigned;
            }

            return WorkflowGuidanceMessages.OverrideStepNotWaiting;
        }

        return null;
    }

    public static string? BuildAddCommentBlockedReason(bool sessionActive, bool notAdmitted)
    {
        if (!notAdmitted)
        {
            return WorkflowGuidanceMessages.CommentAfterAdmitted;
        }

        if (!sessionActive)
        {
            return WorkflowGuidanceMessages.SessionArchivedComments;
        }

        return null;
    }

    public static string? BuildSelectSupervisorBlockedReason(
        bool showSection,
        bool sessionActive,
        SupervisorAssignmentStatus supervisorStatus,
        bool hasEmployees,
        Guid? supervisorId = null)
    {
        if (!showSection)
        {
            return null;
        }

        if (!sessionActive)
        {
            return WorkflowGuidanceMessages.SessionArchivedActions;
        }

        if (SupervisorAssignmentRules.HasPendingRequest(supervisorStatus, supervisorId))
        {
            return WorkflowGuidanceMessages.SupervisorPending;
        }

        if (supervisorStatus == SupervisorAssignmentStatus.Confirmed)
        {
            return WorkflowGuidanceMessages.SupervisorConfirmed;
        }

        if (!hasEmployees)
        {
            return WorkflowGuidanceMessages.NoEmployeesStudent;
        }

        return null;
    }

    public static string? BuildSubmitTopicBlockedReason(
        bool showSection,
        bool sessionActive,
        SupervisorAssignmentStatus supervisorStatus,
        IEnumerable<DiplomaTopicVersion> topicVersions,
        Guid? supervisorId = null)
    {
        if (!showSection)
        {
            return null;
        }

        if (!sessionActive)
        {
            return WorkflowGuidanceMessages.SessionArchivedTopic;
        }

        if (supervisorStatus != SupervisorAssignmentStatus.Confirmed)
        {
            if (SupervisorAssignmentRules.HasPendingRequest(supervisorStatus, supervisorId))
            {
                return WorkflowGuidanceMessages.TopicAwaitSupervisor;
            }

            return WorkflowGuidanceMessages.TopicSelectSupervisor;
        }

        if (topicVersions.Any(version => version.Status == TopicVersionStatus.Approved))
        {
            return WorkflowGuidanceMessages.TopicAlreadyApproved;
        }

        DiplomaTopicVersion? latestTopic = topicVersions
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();

        if (latestTopic?.Status is TopicVersionStatus.PendingSupervisor)
        {
            return WorkflowGuidanceMessages.TopicPendingSupervisor;
        }

        if (latestTopic?.Status is TopicVersionStatus.PendingHead)
        {
            return WorkflowGuidanceMessages.TopicPendingHead;
        }

        if (latestTopic?.Status is TopicVersionStatus.Rejected)
        {
            return WorkflowGuidanceMessages.TopicRejectedResubmit;
        }

        return null;
    }

    private static string? BuildTopicApprovalBlockedReason(IEnumerable<DiplomaTopicVersion> topicVersions)
    {
        DiplomaTopicVersion? latestTopic = topicVersions
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();

        if (latestTopic is null)
        {
            return WorkflowGuidanceMessages.StudentNoTopic;
        }

        return latestTopic.Status switch
        {
            TopicVersionStatus.PendingSupervisor => WorkflowGuidanceMessages.TopicAwaitSupervisorLong,
            TopicVersionStatus.PendingHead => WorkflowGuidanceMessages.TopicAwaitHeadLong,
            TopicVersionStatus.Rejected => WorkflowGuidanceMessages.TopicRejectedStudent,
            _ => WorkflowGuidanceMessages.TopicApprovalRequired,
        };
    }
}
