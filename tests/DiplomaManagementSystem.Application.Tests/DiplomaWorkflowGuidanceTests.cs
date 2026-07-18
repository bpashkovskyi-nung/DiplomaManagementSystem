using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests;

public sealed class DiplomaWorkflowGuidanceTests
{
    [Fact]
    public void BuildAssignReviewerBlockedReason_WhenNoTopic_ReturnsStudentMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: false,
            hasEmployees: true,
            topicVersions: [],
            reviewAssignmentStatus: ReviewAssignmentStatus.NotAssigned);

        Assert.Equal("Студент ще не подав тему роботи.", reason);
    }

    [Fact]
    public void BuildAssignReviewerBlockedReason_WhenTopicApproved_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: true,
            hasEmployees: true,
            topicVersions: [],
            reviewAssignmentStatus: ReviewAssignmentStatus.NotAssigned);

        Assert.Null(reason);
    }

    [Fact]
    public void BuildOverrideAdmissionStepBlockedReason_WhenReviewNotStarted_ReturnsWorkReadyHint()
    {
        Diploma diploma = new() { CurrentAdmissionStep = null };

        string? reason = DiplomaWorkflowGuidance.BuildOverrideAdmissionStepBlockedReason(
            showSection: true,
            sessionActive: true,
            admissionReviewStarted: false,
            diploma,
            attempts: []);

        Assert.Contains("готовність документів", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildOverrideAdmissionStepBlockedReason_WhenStepAlreadyPassing_ReturnsCompletedHint()
    {
        Diploma diploma = new() { CurrentAdmissionStep = AdmissionStep.SupervisorFeedback };
        List<DiplomaAdmissionStepAttempt> attempts =
        [
            new()
            {
                Step = AdmissionStep.SupervisorFeedback,
                AttemptNumber = 1,
                Outcome = CheckpointOutcome.Approved,
            },
        ];

        string? reason = DiplomaWorkflowGuidance.BuildOverrideAdmissionStepBlockedReason(
            showSection: true,
            sessionActive: true,
            admissionReviewStarted: true,
            diploma,
            attempts);

        Assert.Contains("вже пройдено", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSelectSupervisorBlockedReason_WhenPending_ReturnsWaitingMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSelectSupervisorBlockedReason(
            showSection: true,
            sessionActive: true,
            supervisorStatus: SupervisorAssignmentStatus.Pending,
            hasEmployees: true,
            supervisorId: Guid.NewGuid());

        Assert.Contains("очікуйте підтвердження", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildSelectSupervisorBlockedReason_WhenNotSelected_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSelectSupervisorBlockedReason(
            showSection: true,
            sessionActive: true,
            supervisorStatus: SupervisorAssignmentStatus.Pending,
            hasEmployees: true,
            supervisorId: null);

        Assert.Null(reason);
    }

    [Fact]
    public void BuildAdmitBlockedReason_WhenNotReady_ListsBlockers()
    {
        Diploma diploma = new()
        {
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Pending,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
            LifecycleStatus = DiplomaLifecycleStatus.AwaitingSupervisor,
        };

        string? reason = DiplomaWorkflowGuidance.BuildAdmitBlockedReason(
            showSection: true,
            sessionActive: true,
            diploma,
            topicVersions: [],
            attempts: []);

        Assert.Contains("керівник не підтверджений", reason, StringComparison.Ordinal);
        Assert.Contains("тема не затверджена", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildAddCommentBlockedReason_WhenAdmitted_ReturnsAdmittedMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAddCommentBlockedReason(
            sessionActive: true,
            notAdmitted: false);

        Assert.Contains("допущено", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-001
    [Fact]
    public void AssignReviewer_HiddenSection_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: false,
            hasApprovedTopic: false,
            hasEmployees: false,
            topicVersions: [],
            reviewAssignmentStatus: ReviewAssignmentStatus.NotAssigned);

        Assert.Null(reason);
    }

    // TC-APP-GUI-002
    [Fact]
    public void AssignReviewer_NoEmployees_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: true,
            hasEmployees: false,
            topicVersions: [],
            reviewAssignmentStatus: ReviewAssignmentStatus.NotAssigned);

        Assert.Contains("викладач", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-005
    [Fact]
    public void AssignReviewer_AlreadyAssigned_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: true,
            hasEmployees: true,
            topicVersions: [],
            reviewAssignmentStatus: ReviewAssignmentStatus.Assigned);

        Assert.Contains("вже призначено", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-006
    [Fact]
    public void AssignReviewer_ReviewCompleted_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: true,
            hasEmployees: true,
            topicVersions: [],
            reviewAssignmentStatus: ReviewAssignmentStatus.Completed);

        Assert.Contains("завершено", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-007
    [Fact]
    public void DeclareWorkReady_Archived_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildDeclareWorkReadyBlockedReason(
            showSection: true,
            sessionActive: false,
            lifecycleStatus: DiplomaLifecycleStatus.ReviewerAssigned,
            hasStudentWork: true);

        Assert.Contains("архів", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-008
    [Fact]
    public void DeclareWorkReady_WrongLifecycle_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildDeclareWorkReadyBlockedReason(
            showSection: true,
            sessionActive: true,
            lifecycleStatus: DiplomaLifecycleStatus.DocumentsInProgress,
            hasStudentWork: true);

        Assert.Contains("перевірки", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-009
    [Fact]
    public void DeclareWorkReady_NoWork_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildDeclareWorkReadyBlockedReason(
            showSection: true,
            sessionActive: true,
            lifecycleStatus: DiplomaLifecycleStatus.ReviewerAssigned,
            hasStudentWork: false);

        Assert.Contains("завантажте", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-010
    [Fact]
    public void DeclareWorkReady_Valid_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildDeclareWorkReadyBlockedReason(
            showSection: true,
            sessionActive: true,
            lifecycleStatus: DiplomaLifecycleStatus.ReviewerAssigned,
            hasStudentWork: true);

        Assert.Null(reason);
    }

    // TC-APP-GUI-011
    [Fact]
    public void UploadWork_Archived_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildUploadWorkBlockedReason(
            showSection: true,
            sessionActive: false,
            hasApprovedTopic: true,
            lifecycleStatus: DiplomaLifecycleStatus.ReviewerAssigned);

        Assert.Contains("архів", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-012
    [Fact]
    public void UploadWork_NoTopic_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildUploadWorkBlockedReason(
            showSection: true,
            sessionActive: true,
            hasApprovedTopic: false,
            lifecycleStatus: DiplomaLifecycleStatus.ReviewerAssigned);

        Assert.Contains("тем", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-013
    [Fact]
    public void UploadWork_Admitted_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildUploadWorkBlockedReason(
            showSection: true,
            sessionActive: true,
            hasApprovedTopic: true,
            lifecycleStatus: DiplomaLifecycleStatus.Admitted);

        Assert.Contains("допущ", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-014
    [Fact]
    public void UploadWork_WrongLifecycle_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildUploadWorkBlockedReason(
            showSection: true,
            sessionActive: true,
            hasApprovedTopic: true,
            lifecycleStatus: DiplomaLifecycleStatus.AwaitingSupervisor);

        Assert.NotNull(reason);
    }

    // TC-APP-GUI-015
    [Fact]
    public void UploadWork_Valid_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildUploadWorkBlockedReason(
            showSection: true,
            sessionActive: true,
            hasApprovedTopic: true,
            lifecycleStatus: DiplomaLifecycleStatus.ReviewerAssigned);

        Assert.Null(reason);
    }

    [Fact]
    public void UploadWork_TopicApproved_ReturnsAwaitReviewerMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildUploadWorkBlockedReason(
            showSection: true,
            sessionActive: true,
            hasApprovedTopic: true,
            lifecycleStatus: DiplomaLifecycleStatus.TopicApproved);

        Assert.Contains("рецензента", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DeclareWorkReady_TopicApproved_ReturnsAwaitReviewerMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildDeclareWorkReadyBlockedReason(
            showSection: true,
            sessionActive: true,
            lifecycleStatus: DiplomaLifecycleStatus.TopicApproved,
            hasStudentWork: false);

        Assert.Contains("рецензента", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-016
    [Fact]
    public void OverrideSupervisor_Archived_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildOverrideSupervisorBlockedReason(
            showSection: true,
            sessionActive: false,
            hasEmployees: true,
            lifecycleStatus: DiplomaLifecycleStatus.AwaitingSupervisor);

        Assert.Contains("архів", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-017
    [Fact]
    public void OverrideSupervisor_AfterTopic_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildOverrideSupervisorBlockedReason(
            showSection: true,
            sessionActive: true,
            hasEmployees: true,
            lifecycleStatus: DiplomaLifecycleStatus.ReviewerAssigned);

        Assert.Contains("тем", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-018
    [Fact]
    public void OverrideSupervisor_Valid_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildOverrideSupervisorBlockedReason(
            showSection: true,
            sessionActive: true,
            hasEmployees: true,
            lifecycleStatus: DiplomaLifecycleStatus.AwaitingSupervisor);

        Assert.Null(reason);
    }

    // TC-APP-GUI-019
    [Fact]
    public void Admit_Ready_ReturnsNull()
    {
        Diploma diploma = new()
        {
            LifecycleStatus = DiplomaLifecycleStatus.ReadyForAdmission,
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
        };

        string? reason = DiplomaWorkflowGuidance.BuildAdmitBlockedReason(
            showSection: true,
            sessionActive: true,
            diploma,
            topicVersions: [CreateApprovedTopic()],
            attempts: CreateAllPassingAttempts());

        Assert.Null(reason);
    }

    // TC-APP-GUI-021
    [Fact]
    public void Admit_Archived_ReturnsMessage()
    {
        Diploma diploma = new() { LifecycleStatus = DiplomaLifecycleStatus.AwaitingSupervisor };

        string? reason = DiplomaWorkflowGuidance.BuildAdmitBlockedReason(
            showSection: true,
            sessionActive: false,
            diploma,
            topicVersions: [],
            attempts: []);

        Assert.Contains("архів", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-024
    [Fact]
    public void OverrideAdmission_ExternalNoReviewer_ReturnsMessage()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            CurrentAdmissionStep = AdmissionStep.ExternalReview,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            CreatePassingAttempt(diplomaId, AdmissionStep.FormattingReview),
            CreatePassingAttempt(diplomaId, AdmissionStep.AntiPlagiarismClearance),
        ];

        string? reason = DiplomaWorkflowGuidance.BuildOverrideAdmissionStepBlockedReason(
            showSection: true,
            sessionActive: true,
            admissionReviewStarted: true,
            diploma,
            attempts);

        Assert.Contains("рецензент", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-025
    [Fact]
    public void OverrideAdmission_Valid_ReturnsNull()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            CurrentAdmissionStep = AdmissionStep.FormattingReview,
        };

        List<DiplomaAdmissionStepAttempt> attempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
        ];

        string? reason = DiplomaWorkflowGuidance.BuildOverrideAdmissionStepBlockedReason(
            showSection: true,
            sessionActive: true,
            admissionReviewStarted: true,
            diploma,
            attempts);

        Assert.Null(reason);
    }

    // TC-APP-GUI-027
    [Fact]
    public void AddComment_Archived_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAddCommentBlockedReason(
            sessionActive: false,
            notAdmitted: true);

        Assert.Contains("архів", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-028
    [Fact]
    public void AddComment_Valid_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAddCommentBlockedReason(
            sessionActive: true,
            notAdmitted: true);

        Assert.Null(reason);
    }

    // TC-APP-GUI-031
    [Fact]
    public void SelectSupervisor_Confirmed_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSelectSupervisorBlockedReason(
            showSection: true,
            sessionActive: true,
            supervisorStatus: SupervisorAssignmentStatus.Confirmed,
            hasEmployees: true);

        Assert.Contains("підтверджен", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-032
    [Fact]
    public void SelectSupervisor_NoEmployees_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSelectSupervisorBlockedReason(
            showSection: true,
            sessionActive: true,
            supervisorStatus: SupervisorAssignmentStatus.Pending,
            hasEmployees: false,
            supervisorId: null);

        Assert.Contains("викладач", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-033
    [Fact]
    public void SubmitTopic_Archived_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSubmitTopicBlockedReason(
            showSection: true,
            sessionActive: false,
            supervisorStatus: SupervisorAssignmentStatus.Confirmed,
            topicVersions: []);

        Assert.Contains("архів", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-034
    [Fact]
    public void SubmitTopic_NoSupervisor_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSubmitTopicBlockedReason(
            showSection: true,
            sessionActive: true,
            supervisorStatus: SupervisorAssignmentStatus.Pending,
            topicVersions: [],
            supervisorId: null);

        Assert.Contains("керівник", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-035
    [Fact]
    public void SubmitTopic_PendingSupervisor_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSubmitTopicBlockedReason(
            showSection: true,
            sessionActive: true,
            supervisorStatus: SupervisorAssignmentStatus.Pending,
            topicVersions: [],
            supervisorId: Guid.NewGuid());

        Assert.Contains("керівник", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-036
    [Fact]
    public void SubmitTopic_AlreadyApproved_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSubmitTopicBlockedReason(
            showSection: true,
            sessionActive: true,
            supervisorStatus: SupervisorAssignmentStatus.Confirmed,
            topicVersions: [CreateApprovedTopic()]);

        Assert.Contains("затвердж", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-037
    [Fact]
    public void SubmitTopic_PendingHead_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSubmitTopicBlockedReason(
            showSection: true,
            sessionActive: true,
            supervisorStatus: SupervisorAssignmentStatus.Confirmed,
            topicVersions:
            [
                new DiplomaTopicVersion
                {
                    VersionNumber = 1,
                    Status = TopicVersionStatus.PendingHead,
                    Title = "Тема",
                },
            ]);

        Assert.Contains("завідувач", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-038
    [Fact]
    public void SubmitTopic_Rejected_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSubmitTopicBlockedReason(
            showSection: true,
            sessionActive: true,
            supervisorStatus: SupervisorAssignmentStatus.Confirmed,
            topicVersions:
            [
                new DiplomaTopicVersion
                {
                    VersionNumber = 1,
                    Status = TopicVersionStatus.Rejected,
                    Title = "Тема",
                },
            ]);

        Assert.Contains("відхил", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-039
    [Fact]
    public void SubmitTopic_Valid_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildSubmitTopicBlockedReason(
            showSection: true,
            sessionActive: true,
            supervisorStatus: SupervisorAssignmentStatus.Confirmed,
            topicVersions: []);

        Assert.Null(reason);
    }

    // TC-APP-GUI-040
    [Fact]
    public void AssignReviewer_PendingSupervisorTopic_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: false,
            hasEmployees: true,
            topicVersions:
            [
                new DiplomaTopicVersion
                {
                    VersionNumber = 1,
                    Status = TopicVersionStatus.PendingSupervisor,
                    Title = "Тема",
                },
            ],
            reviewAssignmentStatus: ReviewAssignmentStatus.NotAssigned);

        Assert.Contains("керівник", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-041
    [Fact]
    public void AssignReviewer_PendingHeadTopic_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: false,
            hasEmployees: true,
            topicVersions:
            [
                new DiplomaTopicVersion
                {
                    VersionNumber = 1,
                    Status = TopicVersionStatus.PendingHead,
                    Title = "Тема",
                },
            ],
            reviewAssignmentStatus: ReviewAssignmentStatus.NotAssigned);

        Assert.Contains("завідувач", reason, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-GUI-042
    [Fact]
    public void AssignReviewer_RejectedTopic_ReturnsMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: false,
            hasEmployees: true,
            topicVersions:
            [
                new DiplomaTopicVersion
                {
                    VersionNumber = 1,
                    Status = TopicVersionStatus.Rejected,
                    Title = "Тема",
                },
            ],
            reviewAssignmentStatus: ReviewAssignmentStatus.NotAssigned);

        Assert.Contains("відхил", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildAdmitBlockedReason_WhenAllStepsPassButLifecycleNotReady_ReturnsUpdatingMessage()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
            LifecycleStatus = DiplomaLifecycleStatus.DocumentsInProgress,
            CurrentAdmissionStep = null,
        };

        string? reason = DiplomaWorkflowGuidance.BuildAdmitBlockedReason(
            showSection: true,
            sessionActive: true,
            diploma,
            topicVersions: [CreateApprovedTopic()],
            CreateAllPassingAttempts());

        Assert.Contains("оновлюються", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildOverrideAdmissionStepBlockedReason_WhenReviewerAssignmentStep_ReturnsWrongStepType()
    {
        Diploma diploma = new()
        {
            CurrentAdmissionStep = AdmissionStep.ReviewerAssignment,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        string? reason = DiplomaWorkflowGuidance.BuildOverrideAdmissionStepBlockedReason(
            showSection: true,
            sessionActive: true,
            admissionReviewStarted: true,
            diploma,
            attempts: []);

        Assert.Contains("кроків", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildOverrideAdmissionStepBlockedReason_WhenPriorStepsIncomplete_ReturnsNotWaiting()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            CurrentAdmissionStep = AdmissionStep.AntiPlagiarismClearance,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        };

        string? reason = DiplomaWorkflowGuidance.BuildOverrideAdmissionStepBlockedReason(
            showSection: true,
            sessionActive: true,
            admissionReviewStarted: true,
            diploma,
            attempts: [CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback)]);

        Assert.Contains("очікує", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildAssignReviewerBlockedReason_WhenNoTopicVersions_ReturnsNoTopicMessage()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: false,
            hasEmployees: true,
            topicVersions: [],
            reviewAssignmentStatus: ReviewAssignmentStatus.NotAssigned);

        Assert.Contains("тем", reason, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildAssignReviewerBlockedReason_WhenTopicAlreadyApproved_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: true,
            hasEmployees: true,
            topicVersions:
            [
                new DiplomaTopicVersion
                {
                    VersionNumber = 1,
                    Status = TopicVersionStatus.Approved,
                    Title = "Тема",
                },
            ],
            reviewAssignmentStatus: ReviewAssignmentStatus.NotAssigned);

        Assert.Null(reason);
    }

    [Fact]
    public void BuildDeclareWorkReadyBlockedReason_HiddenSection_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildDeclareWorkReadyBlockedReason(
            showSection: false,
            sessionActive: true,
            DiplomaLifecycleStatus.ReviewerAssigned,
            hasStudentWork: true);

        Assert.Null(reason);
    }

    [Fact]
    public void BuildUploadWorkBlockedReason_DocumentsInProgress_ReturnsNull()
    {
        string? reason = DiplomaWorkflowGuidance.BuildUploadWorkBlockedReason(
            showSection: true,
            sessionActive: true,
            hasApprovedTopic: true,
            DiplomaLifecycleStatus.DocumentsInProgress);

        Assert.Null(reason);
    }

    private static DiplomaTopicVersion CreateApprovedTopic() => new()
    {
        VersionNumber = 1,
        Status = TopicVersionStatus.Approved,
        Title = "Тема",
    };

    private static List<DiplomaAdmissionStepAttempt> CreateAllPassingAttempts()
    {
        Guid diplomaId = Guid.NewGuid();
        return
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            CreatePassingAttempt(diplomaId, AdmissionStep.FormattingReview),
            CreatePassingAttempt(diplomaId, AdmissionStep.AntiPlagiarismClearance),
            CreatePassingAttempt(diplomaId, AdmissionStep.ExternalReview),
        ];
    }

    private static DiplomaAdmissionStepAttempt CreatePassingAttempt(Guid diplomaId, AdmissionStep step) => new()
    {
        DiplomaId = diplomaId,
        Step = step,
        AttemptNumber = 1,
        Outcome = CheckpointOutcome.Approved,
    };
}
