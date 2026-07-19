namespace DiplomaManagementSystem.Application.Authorization;

public enum DiplomaAction
{
    ConfirmSupervisor,
    RejectSupervisor,
    ApproveTopicAsSupervisor,
    RejectTopicAsSupervisor,
    CompleteSupervisorCheckpoint,
    CompleteExternalReview,
    CompleteAntiPlagiarism,
    CompleteFormattingReview,
    ApproveTopicAsDepartmentHead,
    RejectTopicAsDepartmentHead,
    AssignReviewer,
    AdmitDiploma,
    ConfirmDefenceDate,
    OverrideSupervisor,
    AddSecretaryComment,
    OverrideAdmissionStep,
    RecordMilestoneProgress,
    RequestDefenceDate,
}
