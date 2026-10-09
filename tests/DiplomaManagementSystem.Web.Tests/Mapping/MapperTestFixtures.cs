using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Application.ReadModels;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Application.Student;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Web.Tests.Mapping;

internal static class MapperTestFixtures
{
    public static readonly Guid DiplomaId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SessionId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SupervisorId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public static readonly Guid ReviewerId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    public static readonly Guid EmployeeId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    public static readonly Guid VersionId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    public static readonly DateTimeOffset Now = new(2026, 3, 15, 10, 0, 0, TimeSpan.Zero);

    public static DiplomaDetailsDto CreateDiplomaDetailsDto() => new(
        Header: new DiplomaDetailsHeaderDto(
            DiplomaId,
            SessionId,
            DefenceSessionType.Bachelor,
            "Іван Іваненко",
            "ivan@test.local",
            "КН-41"),
        Assignments: new DiplomaAssignmentsDto(
            SupervisorId,
            "Петро Петренко",
            SupervisorAssignmentStatus.Confirmed,
            ReviewerId,
            "Олена Коваленко",
            ReviewAssignmentStatus.Assigned),
        State: new DiplomaLifecycleSnapshotDto(
            DiplomaLifecycleStatus.DocumentsInProgress,
            DiplomaAdmissionStatus.NotAdmitted,
            AdmissionStep.FormattingReview,
            null),
        History: new DiplomaDetailsHistoryDto(
            AdmissionSteps:
            [
                new AdmissionStepStatusDto(
                    AdmissionStep.SupervisorFeedback,
                    IsPassing: true,
                    CheckpointOutcome.Approved,
                    null,
                    null,
                    "Петро Петренко",
                    Now,
                    false,
                    1),
            ],
            AttemptHistory:
            [
                new AdmissionStepAttemptDto(
                    AdmissionStep.SupervisorFeedback,
                    1,
                    CheckpointOutcome.Approved,
                    null,
                    "Петро Петренко",
                    Now,
                    false),
            ],
            TopicVersions:
            [
                new SecretaryTopicVersionDto(
                    VersionId,
                    1,
                    "Тема роботи",
                    TopicVersionStatus.Approved,
                    null,
                    Now,
                    Now,
                    "Завідувач",
                    Now,
                    "Петро Петренко"),
            ],
            Comments:
            [
                new DiplomaCommentDto("Секретар", "Коментар", Now),
            ]),
        Actions: new DiplomaWorkflowSecretaryFlags(
            ShowOverrideSupervisorSection: true,
            CanOverrideSupervisor: false,
            OverrideSupervisorBlockedReason: "blocked",
            ShowAssignReviewerSection: true,
            CanAssignReviewer: true,
            AssignReviewerBlockedReason: null,
            ShowAdmitSection: true,
            CanAdmit: false,
            AdmitBlockedReason: "not ready",
            ShowOverrideAdmissionStepSection: true,
            CanOverrideAdmissionStep: false,
            OverrideAdmissionStepBlockedReason: null,
            ShowAddCommentSection: true,
            CanAddComment: true,
            AddCommentBlockedReason: null),
        WorkflowProgress: CreateWorkflowProgress(),
        Documents: CreateDocumentsBundle(),
        EmployeePool: [new PersonOptionDto(EmployeeId, "Співробітник", "emp@test.local")]);

    public static MyDiplomaDto CreateMyDiplomaDto() => new(
        Header: new MyDiplomaHeaderDto(DiplomaId, true, "2026 — Бакалавр", DefenceSessionType.Bachelor),
        Assignments: new MyDiplomaAssignmentsDto(
            "Петро Петренко",
            SupervisorId,
            SupervisorAssignmentStatus.Confirmed,
            "Тема роботи",
            TopicVersionStatus.Approved),
        State: new DiplomaLifecycleSnapshotDto(
            DiplomaLifecycleStatus.WorkInProgressByStudent,
            DiplomaAdmissionStatus.NotAdmitted,
            null,
            null),
        History: new MyDiplomaHistoryDto(
            Checkpoints:
            [
                new StudentAdmissionStepDto(AdmissionStep.SupervisorFeedback, true, false, false),
            ],
            TopicVersions:
            [
                new StudentTopicVersionDto(
                    VersionId,
                    1,
                    "Тема роботи",
                    TopicVersionStatus.Approved,
                    null,
                    Now,
                    Now,
                    "Завідувач",
                    Now,
                    "Керівник"),
            ],
            Comments: [new DiplomaCommentDto("Секретар", "Коментар", Now)]),
        Actions: new DiplomaWorkflowStudentFlags(
            ShowSupervisorSection: false,
            CanSelectSupervisor: false,
            SelectSupervisorBlockedReason: null,
            ShowTopicSubmissionSection: false,
            CanSubmitTopic: false,
            SubmitTopicBlockedReason: null,
            ShowCheckpointsSection: false,
            ShowWorkReadinessSection: true,
            CanDeclareWorkReady: true,
            DeclareWorkReadyBlockedReason: null,
            ShowWorkUploadSection: true,
            CanUploadWork: true,
            UploadWorkBlockedReason: null),
        WorkflowProgress: CreateWorkflowProgress(),
        Documents: CreateDocumentsBundle(),
        SupervisorPool: [new PersonOptionDto(SupervisorId, "Петро Петренко", "petro@test.local")]);

    public static MyDiplomaDto CreateEmptyMyDiplomaDto() => new(
        Header: new MyDiplomaHeaderDto(null, false, null, null),
        Assignments: new MyDiplomaAssignmentsDto(null, null, null, null, null),
        State: null,
        History: new MyDiplomaHistoryDto([], [], []),
        Actions: null,
        WorkflowProgress: null,
        Documents: null,
        SupervisorPool: []);

    public static StudentWorkflowProgressDto CreateWorkflowProgress() => new(
        Steps:
        [
            new StudentWorkflowStepDto(
                1,
                "Вибір керівника",
                StudentWorkflowStepState.Completed,
                "Петро (Підтверджено)",
                null,
                new StudentWorkflowStepStatusDto(
                    "Виконано",
                    "bg-success",
                    "Петро",
                    Now,
                    CheckpointOutcome.Approved,
                    null,
                    false)),
            new StudentWorkflowStepDto(
                2,
                "Тема на розгляді",
                StudentWorkflowStepState.Current,
                "Тема",
                "Подайте тему",
                null),
        ],
        CompletedCount: 1,
        TotalCount: 8,
        ProgressPercent: 12,
        CurrentStepHint: "Подайте тему роботи");

    public static DiplomaDocumentsBundleDto CreateDocumentsBundle() => new(
        StudentWorkVersions: [CreateDocument(DiplomaDocumentKind.StudentWork, 1)],
        LatestSupervisorFeedback: CreateDocument(DiplomaDocumentKind.SupervisorFeedback, 1),
        LatestExternalReview: CreateDocument(DiplomaDocumentKind.ExternalReview, 1),
        LatestAntiPlagiarismReport: CreateDocument(DiplomaDocumentKind.AntiPlagiarismReport, 1));

    public static DiplomaDocumentDto CreateDocument(DiplomaDocumentKind kind, int version) => new(
        Guid.NewGuid(),
        kind,
        version,
        "file.pdf",
        "/files/view/1",
        1024,
        Now,
        null);
}
