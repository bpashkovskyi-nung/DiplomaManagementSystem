using DiplomaManagementSystem.Application.Student;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests;

public sealed class StudentWorkflowProgressBuilderTests
{
    [Fact]
    public void Build_WhenAwaitingSupervisor_CurrentStepIsSupervisor()
    {
        Diploma diploma = CreateDiploma();

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        Assert.Equal(0, progress.CompletedCount);
        Assert.Equal(StudentWorkflowStepState.Current, progress.Steps[0].State);
        Assert.Equal(StudentWorkflowStepState.Upcoming, progress.Steps[1].State);
        Assert.Contains("Оберіть керівника", progress.CurrentStepHint, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WhenSupervisorRequestPending_ShowsWaitingHint()
    {
        Diploma diploma = CreateDiploma();
        diploma.SupervisorId = Guid.NewGuid();

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        Assert.Contains("підтвердження", progress.CurrentStepHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_WhenSupervisorConfirmed_CurrentStepIsTopic()
    {
        Diploma diploma = CreateDiploma();
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed;
        diploma.LifecycleStatus = DiplomaLifecycleStatus.SupervisorConfirmed;

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        Assert.Equal(1, progress.CompletedCount);
        Assert.Equal(StudentWorkflowStepState.Completed, progress.Steps[0].State);
        Assert.Equal(StudentWorkflowStepState.Current, progress.Steps[1].State);
        Assert.Contains("тему", progress.CurrentStepHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_WhenAdmitted_AllStepsCompleted()
    {
        Diploma diploma = CreateAdmittedDiploma();

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        Assert.Equal(8, progress.CompletedCount);
        Assert.Equal(100, progress.ProgressPercent);
        Assert.All(progress.Steps, step => Assert.Equal(StudentWorkflowStepState.Completed, step.State));
        Assert.Contains("допущені", progress.CurrentStepHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_ForSecretaryAtReviewStep_SuggestsAssignReviewer()
    {
        Diploma diploma = CreateDiploma();
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed;
        diploma.LifecycleStatus = DiplomaLifecycleStatus.DocumentsInProgress;
        diploma.CurrentAdmissionStep = AdmissionStep.ReviewerAssignment;
        Guid diplomaId = diploma.Id;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        diploma.TopicVersions =
        [
            new DiplomaTopicVersion
            {
                Id = Guid.NewGuid(),
                DiplomaId = diplomaId,
                VersionNumber = 1,
                Title = "Тема",
                Status = TopicVersionStatus.Approved,
                SubmittedAt = now,
            },
        ];
        diploma.AdmissionStepAttempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            CreatePassingAttempt(diplomaId, AdmissionStep.FormattingReview),
            CreatePassingAttempt(diplomaId, AdmissionStep.AntiPlagiarismClearance),
        ];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(
            diploma,
            sessionActive: true,
            WorkflowAudience.Secretary);

        Assert.Equal(StudentWorkflowStepState.Current, progress.Steps[6].State);
        Assert.Contains("Призначте рецензента", progress.CurrentStepHint, StringComparison.Ordinal);
    }

    // TC-APP-SWP-006
    [Fact]
    public void Build_WhenSessionArchived_AppendsArchivedSuffixToHint()
    {
        Diploma diploma = CreateDiploma();

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: false);

        Assert.NotNull(progress.CurrentStepHint);
        Assert.Contains("архів", progress.CurrentStepHint, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-SWP-007
    [Fact]
    public void Build_WhenTopicInReview_CurrentStepIsTopicSubmission()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = CreateDiploma();
        diploma.Id = diplomaId;
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed;
        diploma.TopicVersions =
        [
            new DiplomaTopicVersion
            {
                DiplomaId = diplomaId,
                VersionNumber = 1,
                Title = "Тема на розгляді",
                Status = TopicVersionStatus.PendingSupervisor,
                SubmittedAt = DateTimeOffset.UtcNow,
            },
        ];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        Assert.Equal(1, progress.CompletedCount);
        Assert.Equal(StudentWorkflowStepState.Current, progress.Steps[1].State);
        Assert.Contains("розглядає", progress.CurrentStepHint, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-SWP-008
    [Fact]
    public void Build_WhenDocumentsInProgress_CurrentStepIsSupervisorFeedback()
    {
        Guid diplomaId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Diploma diploma = CreateDiploma();
        diploma.Id = diplomaId;
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed;
        diploma.CurrentAdmissionStep = AdmissionStep.SupervisorFeedback;
        diploma.TopicVersions =
        [
            new DiplomaTopicVersion
            {
                DiplomaId = diplomaId,
                VersionNumber = 1,
                Title = "Затверджена тема",
                Status = TopicVersionStatus.Approved,
                SubmittedAt = now,
            },
        ];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        Assert.Equal(3, progress.CompletedCount);
        Assert.Equal(StudentWorkflowStepState.Current, progress.Steps[3].State);
        Assert.Contains("керівник", progress.CurrentStepHint, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-SWP-009
    [Fact]
    public void Build_WhenReadyForAdmission_ShowsAdmitHint()
    {
        Guid diplomaId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Diploma diploma = CreateDiploma();
        diploma.Id = diplomaId;
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed;
        diploma.ReviewAssignmentStatus = ReviewAssignmentStatus.Completed;
        diploma.LifecycleStatus = DiplomaLifecycleStatus.ReadyForAdmission;
        diploma.CurrentAdmissionStep = null;
        diploma.TopicVersions =
        [
            new DiplomaTopicVersion
            {
                DiplomaId = diplomaId,
                VersionNumber = 1,
                Title = "Тема",
                Status = TopicVersionStatus.Approved,
                SubmittedAt = now,
            },
        ];
        diploma.AdmissionStepAttempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            CreatePassingAttempt(diplomaId, AdmissionStep.FormattingReview),
            CreatePassingAttempt(diplomaId, AdmissionStep.AntiPlagiarismClearance),
            CreatePassingAttempt(diplomaId, AdmissionStep.ExternalReview),
        ];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        Assert.Equal(StudentWorkflowStepState.Current, progress.Steps[7].State);
        Assert.Contains("секретар", progress.CurrentStepHint, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-SWP-010
    [Fact]
    public void Build_ForSecretaryWithPendingSupervisor_ShowsWaitingHint()
    {
        Diploma diploma = CreateDiploma();
        diploma.SupervisorId = Guid.NewGuid();

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(
            diploma,
            sessionActive: true,
            WorkflowAudience.Secretary);

        Assert.Contains("підтвердження", progress.CurrentStepHint, StringComparison.OrdinalIgnoreCase);
    }

    // TC-APP-SWP-011
    [Fact]
    public void Build_WhenTopicApproved_IncludesApprovalDetailOnTopicReviewStep()
    {
        Guid diplomaId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Diploma diploma = CreateDiploma();
        diploma.Id = diplomaId;
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed;
        diploma.TopicVersions =
        [
            new DiplomaTopicVersion
            {
                DiplomaId = diplomaId,
                VersionNumber = 1,
                Title = "Тема",
                Status = TopicVersionStatus.Approved,
                SubmittedAt = now,
                SupervisorReviewedAt = now,
                ReviewedAt = now,
            },
        ];
        diploma.CurrentAdmissionStep = AdmissionStep.SupervisorFeedback;

        TopicApprovalDisplay approval = new("Керівник: Петро — погоджено", "Завідувач: Олена — 15.03.2026");
        WorkflowPersonLabels people = new(TopicApproval: approval);

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(
            diploma,
            sessionActive: true,
            people: people);

        StudentWorkflowStepDto approvedTopicStep = progress.Steps[1];
        Assert.Equal(StudentWorkflowStepState.Completed, approvedTopicStep.State);
        Assert.Equal("Тема на розгляді", approvedTopicStep.Title);
        Assert.NotNull(approvedTopicStep.Detail);
        Assert.Contains("Тема", approvedTopicStep.Detail, StringComparison.Ordinal);
        Assert.Contains("Петро", approvedTopicStep.Detail, StringComparison.Ordinal);
        Assert.Contains("Олена", approvedTopicStep.Detail, StringComparison.Ordinal);

        StudentWorkflowStepDto workStep = progress.Steps[2];
        Assert.Equal("Робота виконується", workStep.Title);
        Assert.Equal("Виконано", workStep.Detail);
    }

    // TC-APP-SWP-012
    [Fact]
    public void Build_WhenCheckpointRejected_ShowsRejectedBadge()
    {
        Guid diplomaId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Diploma diploma = CreateDiploma();
        diploma.Id = diplomaId;
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed;
        diploma.CurrentAdmissionStep = AdmissionStep.FormattingReview;
        diploma.TopicVersions =
        [
            new DiplomaTopicVersion
            {
                DiplomaId = diplomaId,
                VersionNumber = 1,
                Title = "Тема",
                Status = TopicVersionStatus.Approved,
                SubmittedAt = now,
            },
        ];
        diploma.AdmissionStepAttempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            new DiplomaAdmissionStepAttempt
            {
                Id = Guid.NewGuid(),
                DiplomaId = diplomaId,
                Step = AdmissionStep.FormattingReview,
                AttemptNumber = 1,
                Outcome = CheckpointOutcome.NotApproved,
                Comment = "Потрібні виправлення",
                RecordedAt = now,
            },
        ];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        StudentWorkflowStepStatusDto? status = progress.Steps[4].Status;
        Assert.NotNull(status);
        Assert.Equal("Відхилено", status.BadgeText);
        Assert.Equal("bg-danger", status.BadgeCssClass);
        Assert.Equal("Потрібні виправлення", status.Comment);
    }

    // TC-APP-SWP-013
    [Fact]
    public void Build_WhenCheckpointPassing_ShowsCompletedBadgeAndRecorder()
    {
        Guid diplomaId = Guid.NewGuid();
        Guid recorderId = Guid.NewGuid();
        DateTimeOffset recordedAt = DateTimeOffset.UtcNow;
        Diploma diploma = CreateDocumentsInProgressDiploma(diplomaId);
        diploma.CurrentAdmissionStep = AdmissionStep.FormattingReview;
        diploma.AdmissionStepAttempts =
        [
            new DiplomaAdmissionStepAttempt
            {
                Id = Guid.NewGuid(),
                DiplomaId = diplomaId,
                Step = AdmissionStep.SupervisorFeedback,
                AttemptNumber = 1,
                Outcome = CheckpointOutcome.Approved,
                RecordedById = recorderId,
                RecordedAt = recordedAt,
            },
        ];

        Dictionary<Guid, string> completedByNames = new() { [recorderId] = "Іван Керівник" };

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(
            diploma,
            sessionActive: true,
            completedByNames: completedByNames);

        StudentWorkflowStepStatusDto? status = progress.Steps[3].Status;
        Assert.NotNull(status);
        Assert.Equal("Виконано", status.BadgeText);
        Assert.Equal("bg-success", status.BadgeCssClass);
        Assert.Equal("Іван Керівник", status.CompletedByName);
        Assert.Equal(recordedAt, status.CompletedAt);
    }

    // TC-APP-SWP-014
    [Fact]
    public void Build_WhenCheckpointLocked_ShowsWaitingForPriorBadge()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = CreateDocumentsInProgressDiploma(diplomaId);
        diploma.CurrentAdmissionStep = AdmissionStep.AntiPlagiarismClearance;
        diploma.AdmissionStepAttempts = [];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        StudentWorkflowStepStatusDto? status = progress.Steps[4].Status;
        Assert.NotNull(status);
        Assert.Equal("Очікує попередніх", status.BadgeText);
        Assert.Equal("bg-secondary", status.BadgeCssClass);
    }

    // TC-APP-SWP-015
    [Fact]
    public void Build_WhenAssignedReviewer_CurrentExternalReviewBadge()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = CreateDocumentsInProgressDiploma(diplomaId);
        diploma.ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned;
        diploma.CurrentAdmissionStep = AdmissionStep.ReviewerAssignment;
        diploma.AdmissionStepAttempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            CreatePassingAttempt(diplomaId, AdmissionStep.FormattingReview),
            CreatePassingAttempt(diplomaId, AdmissionStep.AntiPlagiarismClearance),
        ];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        StudentWorkflowStepStatusDto? status = progress.Steps[6].Status;
        Assert.NotNull(status);
        Assert.Equal("Поточний етап", status.BadgeText);
        Assert.Equal("bg-warning text-dark", status.BadgeCssClass);
    }

    // TC-APP-SWP-016
    [Fact]
    public void Build_WhenSecretaryOverrideAttempt_FlagsStatus()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = CreateDocumentsInProgressDiploma(diplomaId);
        diploma.CurrentAdmissionStep = AdmissionStep.FormattingReview;
        diploma.AdmissionStepAttempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            new DiplomaAdmissionStepAttempt
            {
                Id = Guid.NewGuid(),
                DiplomaId = diplomaId,
                Step = AdmissionStep.FormattingReview,
                AttemptNumber = 1,
                Outcome = CheckpointOutcome.Approved,
                IsSecretaryOverride = true,
            },
        ];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        StudentWorkflowStepStatusDto? status = progress.Steps[4].Status;
        Assert.NotNull(status);
        Assert.True(status.IsSecretaryOverride);
    }

    // TC-APP-SWP-017
    [Fact]
    public void Build_WhenAdmittedForSecretary_IncludesDefenceDateInHint()
    {
        Diploma diploma = CreateAdmittedDiploma();

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(
            diploma,
            sessionActive: true,
            WorkflowAudience.Secretary);

        Assert.Contains("Студента допущено", progress.CurrentStepHint, StringComparison.Ordinal);
        Assert.Contains("2026", progress.CurrentStepHint, StringComparison.Ordinal);
    }

    // TC-APP-SWP-018
    [Fact]
    public void Build_WhenSupervisorConfirmed_ShowsSupervisorNameInDetail()
    {
        Diploma diploma = CreateDiploma();
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed;

        WorkflowPersonLabels people = new(SupervisorName: "Олена Коваленко");

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(
            diploma,
            sessionActive: true,
            people: people);

        Assert.Contains("Олена Коваленко", progress.Steps[0].Detail, StringComparison.Ordinal);
        Assert.Contains("Підтверджено", progress.Steps[0].Detail, StringComparison.Ordinal);
    }

    // TC-APP-SWP-019
    [Fact]
    public void Build_WhenReviewerAssigned_ShowsReviewerNameInDetail()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = CreateDocumentsInProgressDiploma(diplomaId);
        diploma.ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned;
        diploma.CurrentAdmissionStep = AdmissionStep.ExternalReview;
        diploma.AdmissionStepAttempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            CreatePassingAttempt(diplomaId, AdmissionStep.FormattingReview),
            CreatePassingAttempt(diplomaId, AdmissionStep.AntiPlagiarismClearance),
        ];

        WorkflowPersonLabels people = new(ReviewerName: "Петро Рецензент");

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(
            diploma,
            sessionActive: true,
            people: people);

        Assert.Equal("Петро Рецензент", progress.Steps[6].Detail);
    }

    [Fact]
    public void Build_WhenFutureCheckpointWaiting_ShowsWaitingBadge()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = CreateDocumentsInProgressDiploma(diplomaId);
        diploma.CurrentAdmissionStep = AdmissionStep.ExternalReview;
        diploma.ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned;
        diploma.AdmissionStepAttempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            CreatePassingAttempt(diplomaId, AdmissionStep.FormattingReview),
        ];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        StudentWorkflowStepStatusDto? status = progress.Steps[5].Status;
        Assert.NotNull(status);
        Assert.Equal("Очікує", status.BadgeText);
        Assert.Equal("bg-secondary", status.BadgeCssClass);
    }

    [Fact]
    public void Build_ForSecretaryAtFormattingStep_ShowsFormattingHint()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = CreateDocumentsInProgressDiploma(diplomaId);
        diploma.CurrentAdmissionStep = AdmissionStep.FormattingReview;
        diploma.AdmissionStepAttempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
        ];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(
            diploma,
            sessionActive: true,
            WorkflowAudience.Secretary);

        Assert.Contains("Нормоконтролер", progress.CurrentStepHint, StringComparison.Ordinal);
    }

    [Fact]
    public void Build_WhenArchivedDuringWorkPhase_AppendsArchivedSuffix()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = CreateDocumentsInProgressDiploma(diplomaId);
        diploma.LifecycleStatus = DiplomaLifecycleStatus.ReviewerAssigned;

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: false);

        Assert.NotNull(progress.CurrentStepHint);
        Assert.Contains("архів", progress.CurrentStepHint, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_WhenReviewerNotAssigned_ShowsNotAssignedDetail()
    {
        Guid diplomaId = Guid.NewGuid();
        Diploma diploma = CreateDocumentsInProgressDiploma(diplomaId);
        diploma.CurrentAdmissionStep = AdmissionStep.ReviewerAssignment;
        diploma.AdmissionStepAttempts =
        [
            CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
            CreatePassingAttempt(diplomaId, AdmissionStep.FormattingReview),
            CreatePassingAttempt(diplomaId, AdmissionStep.AntiPlagiarismClearance),
        ];

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(diploma, sessionActive: true);

        Assert.Equal("Не призначено", progress.Steps[6].Detail);
    }

    [Fact]
    public void Build_WhenSupervisorRejected_ShowsRejectedDetailWithName()
    {
        Diploma diploma = CreateDiploma();
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Rejected;
        WorkflowPersonLabels people = new(SupervisorName: "Іван Керівник");

        StudentWorkflowProgressDto progress = StudentWorkflowProgressBuilder.Build(
            diploma,
            sessionActive: true,
            people: people);

        Assert.Contains("Відхилено", progress.Steps[0].Detail, StringComparison.Ordinal);
        Assert.Contains("Іван Керівник", progress.Steps[0].Detail, StringComparison.Ordinal);
    }

    private static Diploma CreateDiploma() => new()
    {
        Id = Guid.NewGuid(),
        SupervisorAssignmentStatus = SupervisorAssignmentStatus.Pending,
        ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
        LifecycleStatus = DiplomaLifecycleStatus.AwaitingSupervisor,
        AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
        TopicVersions = [],
        AdmissionStepAttempts = [],
    };

    private static Diploma CreateAdmittedDiploma()
    {
        Guid diplomaId = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;

        Diploma diploma = new()
        {
            Id = diplomaId,
            SupervisorId = Guid.NewGuid(),
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
            LifecycleStatus = DiplomaLifecycleStatus.Admitted,
            AdmissionStatus = DiplomaAdmissionStatus.Admitted,
            CurrentAdmissionStep = null,
            DefenceDate = new DateOnly(2026, 6, 15),
            TopicVersions =
            [
                new DiplomaTopicVersion
                {
                    Id = Guid.NewGuid(),
                    DiplomaId = diplomaId,
                    VersionNumber = 1,
                    Title = "Тема",
                    Status = TopicVersionStatus.Approved,
                    SubmittedAt = now,
                },
            ],
            AdmissionStepAttempts =
            [
                CreatePassingAttempt(diplomaId, AdmissionStep.SupervisorFeedback),
                CreatePassingAttempt(diplomaId, AdmissionStep.FormattingReview),
                CreatePassingAttempt(diplomaId, AdmissionStep.AntiPlagiarismClearance),
                CreatePassingAttempt(diplomaId, AdmissionStep.ExternalReview),
            ],
        };

        return diploma;
    }

    private static Diploma CreateDocumentsInProgressDiploma(Guid diplomaId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new Diploma
        {
            Id = diplomaId,
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
            LifecycleStatus = DiplomaLifecycleStatus.DocumentsInProgress,
            AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
            TopicVersions =
            [
                new DiplomaTopicVersion
                {
                    DiplomaId = diplomaId,
                    VersionNumber = 1,
                    Title = "Тема",
                    Status = TopicVersionStatus.Approved,
                    SubmittedAt = now,
                },
            ],
            AdmissionStepAttempts = [],
        };
    }

    private static DiplomaAdmissionStepAttempt CreatePassingAttempt(Guid diplomaId, AdmissionStep step) =>
        new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diplomaId,
            Step = step,
            AttemptNumber = 1,
            Outcome = CheckpointOutcome.Approved,
        };
}
