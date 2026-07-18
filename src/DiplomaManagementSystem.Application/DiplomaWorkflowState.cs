using DiplomaManagementSystem.Application.Student;
using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Application;

public sealed record DiplomaWorkflowOptions(
    bool HasEmployees,
    bool HasStudentWork = false,
    bool CanSubmitNewTopic = false);

public sealed record DiplomaWorkflowStudentFlags(
    bool ShowSupervisorSection,
    bool CanSelectSupervisor,
    string? SelectSupervisorBlockedReason,
    bool ShowTopicSubmissionSection,
    bool CanSubmitTopic,
    string? SubmitTopicBlockedReason,
    bool ShowCheckpointsSection,
    bool ShowWorkReadinessSection,
    bool CanDeclareWorkReady,
    string? DeclareWorkReadyBlockedReason,
    bool ShowWorkUploadSection,
    bool CanUploadWork,
    string? UploadWorkBlockedReason);

public sealed record DiplomaWorkflowSecretaryFlags(
    bool ShowOverrideSupervisorSection,
    bool CanOverrideSupervisor,
    string? OverrideSupervisorBlockedReason,
    bool ShowAssignReviewerSection,
    bool CanAssignReviewer,
    string? AssignReviewerBlockedReason,
    bool ShowAdmitSection,
    bool CanAdmit,
    string? AdmitBlockedReason,
    bool ShowOverrideAdmissionStepSection,
    bool CanOverrideAdmissionStep,
    string? OverrideAdmissionStepBlockedReason,
    bool ShowAddCommentSection,
    bool CanAddComment,
    string? AddCommentBlockedReason)
{
    public static DiplomaWorkflowSecretaryFlags ReadOnly { get; } = new(
        ShowOverrideSupervisorSection: false,
        CanOverrideSupervisor: false,
        OverrideSupervisorBlockedReason: null,
        ShowAssignReviewerSection: false,
        CanAssignReviewer: false,
        AssignReviewerBlockedReason: null,
        ShowAdmitSection: false,
        CanAdmit: false,
        AdmitBlockedReason: null,
        ShowOverrideAdmissionStepSection: false,
        CanOverrideAdmissionStep: false,
        OverrideAdmissionStepBlockedReason: null,
        ShowAddCommentSection: false,
        CanAddComment: false,
        AddCommentBlockedReason: null);
}

public sealed record DiplomaWorkflowState(
    bool SessionActive,
    bool HasApprovedTopic,
    bool HasEmployees,
    bool AdmissionReviewStarted,
    bool NotAdmitted,
    DiplomaWorkflowStudentFlags? Student,
    DiplomaWorkflowSecretaryFlags? Secretary)
{
    public static DiplomaWorkflowState From(
        Diploma diploma,
        WorkflowAudience audience,
        DiplomaWorkflowOptions options)
    {
        bool sessionActive = diploma.DefenceSession.Status == DefenceSessionStatus.Active;
        bool hasApprovedTopic = diploma.TopicVersions.Any(version => version.Status == TopicVersionStatus.Approved);
        bool hasEmployees = options.HasEmployees;
        bool admissionReviewStarted = diploma.AdmissionStepAttempts.Count > 0
                                      || diploma.CurrentAdmissionStep is not null;
        bool notAdmitted = diploma.AdmissionStatus != DiplomaAdmissionStatus.Admitted;

        DiplomaWorkflowStudentFlags? student = audience == WorkflowAudience.Student
            ? BuildStudentFlags(diploma, sessionActive, hasApprovedTopic, hasEmployees, admissionReviewStarted, options)
            : null;

        DiplomaWorkflowSecretaryFlags? secretary = audience == WorkflowAudience.Secretary
            ? BuildSecretaryFlags(diploma, sessionActive, hasApprovedTopic, hasEmployees, admissionReviewStarted, notAdmitted)
            : null;

        return new DiplomaWorkflowState(
            sessionActive,
            hasApprovedTopic,
            hasEmployees,
            admissionReviewStarted,
            notAdmitted,
            student,
            secretary);
    }

    private static DiplomaWorkflowStudentFlags BuildStudentFlags(
        Diploma diploma,
        bool sessionActive,
        bool hasApprovedTopic,
        bool hasEmployees,
        bool admissionReviewStarted,
        DiplomaWorkflowOptions options)
    {
        bool showSupervisorSection = diploma.SupervisorAssignmentStatus != SupervisorAssignmentStatus.Confirmed;
        bool canSelectSupervisor = sessionActive
                                   && SupervisorAssignmentRules.CanRequestSupervisor(
                                       diploma.SupervisorAssignmentStatus,
                                       diploma.SupervisorId)
                                   && hasEmployees;
        string? selectSupervisorBlockedReason = canSelectSupervisor
            ? null
            : DiplomaWorkflowGuidance.BuildSelectSupervisorBlockedReason(
                showSupervisorSection,
                sessionActive,
                diploma.SupervisorAssignmentStatus,
                hasEmployees,
                diploma.SupervisorId);

        bool showTopicSubmissionSection = !hasApprovedTopic;
        bool canSubmitTopic = sessionActive
                              && diploma.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Confirmed
                              && options.CanSubmitNewTopic;
        string? submitTopicBlockedReason = canSubmitTopic
            ? null
            : DiplomaWorkflowGuidance.BuildSubmitTopicBlockedReason(
                showTopicSubmissionSection,
                sessionActive,
                diploma.SupervisorAssignmentStatus,
                diploma.TopicVersions,
                diploma.SupervisorId);

        bool showCheckpointsSection = admissionReviewStarted;
        bool showWorkReadinessSection = hasApprovedTopic && !admissionReviewStarted;
        bool canDeclareWorkReady = sessionActive
                                   && showWorkReadinessSection
                                   && diploma.LifecycleStatus == DiplomaLifecycleStatus.ReviewerAssigned
                                   && options.HasStudentWork;
        string? declareWorkReadyBlockedReason = canDeclareWorkReady
            ? null
            : DiplomaWorkflowGuidance.BuildDeclareWorkReadyBlockedReason(
                showWorkReadinessSection,
                sessionActive,
                diploma.LifecycleStatus,
                options.HasStudentWork);

        bool showWorkUploadSection = hasApprovedTopic
                                     && sessionActive
                                     && diploma.LifecycleStatus != DiplomaLifecycleStatus.Admitted;
        bool canUploadWork = showWorkUploadSection
                             && DiplomaWorkflowGuidance.BuildUploadWorkBlockedReason(
                                 showWorkUploadSection,
                                 sessionActive,
                                 hasApprovedTopic,
                                 diploma.LifecycleStatus) is null;
        string? uploadWorkBlockedReason = canUploadWork
            ? null
            : DiplomaWorkflowGuidance.BuildUploadWorkBlockedReason(
                showWorkUploadSection,
                sessionActive,
                hasApprovedTopic,
                diploma.LifecycleStatus);

        return new DiplomaWorkflowStudentFlags(
            showSupervisorSection,
            canSelectSupervisor,
            selectSupervisorBlockedReason,
            showTopicSubmissionSection,
            canSubmitTopic,
            submitTopicBlockedReason,
            showCheckpointsSection,
            showWorkReadinessSection,
            canDeclareWorkReady,
            declareWorkReadyBlockedReason,
            showWorkUploadSection,
            canUploadWork,
            uploadWorkBlockedReason);
    }

    private static DiplomaWorkflowSecretaryFlags BuildSecretaryFlags(
        Diploma diploma,
        bool sessionActive,
        bool hasApprovedTopic,
        bool hasEmployees,
        bool admissionReviewStarted,
        bool notAdmitted)
    {
        bool reviewNotCompleted = diploma.ReviewAssignmentStatus != ReviewAssignmentStatus.Completed;

        bool showAssignReviewerSection = sessionActive && notAdmitted && reviewNotCompleted;
        bool canAssignReviewer = showAssignReviewerSection
                                 && hasApprovedTopic
                                 && hasEmployees
                                 && diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.NotAssigned;
        string? assignReviewerBlockedReason = canAssignReviewer
            ? null
            : DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
                showAssignReviewerSection,
                hasApprovedTopic,
                hasEmployees,
                diploma.TopicVersions,
                diploma.ReviewAssignmentStatus);

        bool supervisorChangeAllowed = SupervisorOverridePolicy.AllowsLifecycleOverride(diploma.LifecycleStatus);
        bool showOverrideSupervisorSection = notAdmitted;
        bool canOverrideSupervisor = showOverrideSupervisorSection
                                     && sessionActive
                                     && hasEmployees
                                     && supervisorChangeAllowed;
        string? overrideSupervisorBlockedReason = canOverrideSupervisor
            ? null
            : DiplomaWorkflowGuidance.BuildOverrideSupervisorBlockedReason(
                showOverrideSupervisorSection,
                sessionActive,
                hasEmployees,
                diploma.LifecycleStatus);

        bool showAdmitSection = notAdmitted;
        bool canAdmit = showAdmitSection
                        && sessionActive
                        && diploma.LifecycleStatus == DiplomaLifecycleStatus.ReadyForAdmission;
        string? admitBlockedReason = canAdmit
            ? null
            : DiplomaWorkflowGuidance.BuildAdmitBlockedReason(
                showAdmitSection,
                sessionActive,
                diploma,
                diploma.TopicVersions,
                diploma.AdmissionStepAttempts);

        bool showOverrideAdmissionStepSection = notAdmitted;
        bool canOverrideAdmissionStep = showOverrideAdmissionStepSection
                                        && sessionActive
                                        && admissionReviewStarted
                                        && AdmissionStepStatusResolver.CanSecretaryOverrideCurrentStep(
                                            diploma,
                                            diploma.AdmissionStepAttempts);
        string? overrideAdmissionStepBlockedReason = canOverrideAdmissionStep
            ? null
            : DiplomaWorkflowGuidance.BuildOverrideAdmissionStepBlockedReason(
                showOverrideAdmissionStepSection,
                sessionActive,
                admissionReviewStarted,
                diploma,
                diploma.AdmissionStepAttempts);

        bool showAddCommentSection = true;
        bool canAddComment = sessionActive && notAdmitted;
        string? addCommentBlockedReason = canAddComment
            ? null
            : DiplomaWorkflowGuidance.BuildAddCommentBlockedReason(sessionActive, notAdmitted);

        return new DiplomaWorkflowSecretaryFlags(
            showOverrideSupervisorSection,
            canOverrideSupervisor,
            overrideSupervisorBlockedReason,
            showAssignReviewerSection,
            canAssignReviewer,
            assignReviewerBlockedReason,
            showAdmitSection,
            canAdmit,
            admitBlockedReason,
            showOverrideAdmissionStepSection,
            canOverrideAdmissionStep,
            overrideAdmissionStepBlockedReason,
            showAddCommentSection,
            canAddComment,
            addCommentBlockedReason);
    }
}
