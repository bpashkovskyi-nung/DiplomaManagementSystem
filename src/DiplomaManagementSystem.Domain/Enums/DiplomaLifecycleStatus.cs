namespace DiplomaManagementSystem.Domain.Enums;

public enum DiplomaLifecycleStatus
{
    AwaitingSupervisor = 0,
    SupervisorConfirmed = 1,
    TopicInReview = 2,
    TopicApproved = 3,
    WorkInProgressByStudent = 4,
    DocumentsInProgress = 5,
    ReadyForAdmission = 6,
    Admitted = 7,
}
