using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.ReadModels;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests.Secretary;

public sealed class DiplomaDetailsAssemblerTests
{
    [Fact]
    public void BuildHistory_IncludesTopicVersionsAndAdmissionAttempts()
    {
        var diplomaId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        var headId = Guid.NewGuid();

        DiplomaTopicVersion topic = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diplomaId,
            VersionNumber = 1,
            Title = "Тема роботи",
            Status = TopicVersionStatus.Approved,
            SubmittedAt = DateTimeOffset.UtcNow,
            SupervisorReviewedAt = DateTimeOffset.UtcNow,
            SupervisorReviewedById = supervisorId,
            ReviewedAt = DateTimeOffset.UtcNow,
            ReviewedById = headId,
        };

        DiplomaAdmissionStepAttempt attempt = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diplomaId,
            Step = AdmissionStep.SupervisorFeedback,
            AttemptNumber = 1,
            Outcome = CheckpointOutcome.Approved,
            RecordedById = supervisorId,
            RecordedAt = DateTimeOffset.UtcNow,
        };

        Diploma diploma = new()
        {
            Id = diplomaId,
            StudentId = Guid.NewGuid(),
            DefenceSessionId = Guid.NewGuid(),
            DefenceSession = new DefenceSession
            {
                Id = Guid.NewGuid(),
                Year = 2026,
                Type = DefenceSessionType.Bachelor,
                Status = DefenceSessionStatus.Active,
            },
            LifecycleStatus = DiplomaLifecycleStatus.DocumentsInProgress,
            CurrentAdmissionStep = AdmissionStep.FormattingReview,
            TopicVersions = [topic],
            AdmissionStepAttempts = [attempt],
        };

        ApplicationUser supervisor = new()
        {
            Id = supervisorId,
            FullName = "Supervisor One",
        };

        ApplicationUser head = new()
        {
            Id = headId,
            FullName = "Head One",
        };

        DiplomaDetailsContext context = new(
            diploma,
            new Dictionary<Guid, ApplicationUser>
            {
                [supervisorId] = supervisor,
                [headId] = head,
            },
            Student: new ApplicationUser { Id = diploma.StudentId, FullName = "Student One" },
            StudyGroupName: "КН-41",
            SupervisorName: "Supervisor One",
            ReviewerName: null,
            Comments: [],
            CommentAuthorNames: [],
            EmployeePool: [],
            Documents: new DiplomaDocumentsBundleDto([], null, null, null),
            SessionActive: true);

        DiplomaDetailsHistory history = DiplomaDetailsAssembler.BuildHistory(context);

        SecretaryTopicVersionDto topicDto = Assert.Single(history.TopicVersions);
        Assert.Equal("Тема роботи", topicDto.Title);
        Assert.Equal("Supervisor One", topicDto.SupervisorReviewedByName);
        Assert.Equal("Head One", topicDto.ReviewedByName);

        AdmissionStepStatusDto stepStatus = history.AdmissionSteps
            .Single(step => step.Step == AdmissionStep.SupervisorFeedback);
        Assert.True(stepStatus.IsPassing);
        Assert.Equal("Supervisor One", stepStatus.RecordedByName);
    }

    [Fact]
    public void BuildScreenParts_ExposesSecretaryActionsAndWorkflowProgress()
    {
        var diplomaId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();

        DiplomaTopicVersion topic = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diplomaId,
            VersionNumber = 1,
            Title = "Тема",
            Status = TopicVersionStatus.Approved,
            SubmittedAt = DateTimeOffset.UtcNow,
            SupervisorReviewedAt = DateTimeOffset.UtcNow,
            SupervisorReviewedById = supervisorId,
            ReviewedAt = DateTimeOffset.UtcNow,
        };

        Diploma diploma = new()
        {
            Id = diplomaId,
            StudentId = Guid.NewGuid(),
            DefenceSessionId = Guid.NewGuid(),
            SupervisorId = supervisorId,
            DefenceSession = new DefenceSession
            {
                Id = Guid.NewGuid(),
                Year = 2026,
                Type = DefenceSessionType.Bachelor,
                Status = DefenceSessionStatus.Active,
            },
            LifecycleStatus = DiplomaLifecycleStatus.DocumentsInProgress,
            CurrentAdmissionStep = AdmissionStep.SupervisorFeedback,
            TopicVersions = [topic],
            AdmissionStepAttempts = [],
        };

        ApplicationUser supervisor = new()
        {
            Id = supervisorId,
            FullName = "Supervisor One",
        };

        DiplomaDetailsContext context = new(
            diploma,
            new Dictionary<Guid, ApplicationUser> { [supervisorId] = supervisor },
            Student: new ApplicationUser { Id = diploma.StudentId, FullName = "Student One" },
            StudyGroupName: "КН-41",
            SupervisorName: "Supervisor One",
            ReviewerName: null,
            Comments: [],
            CommentAuthorNames: [],
            EmployeePool: [new PersonOptionDto(Guid.NewGuid(), "Employee One", "employee@test.local")],
            Documents: new DiplomaDocumentsBundleDto([], null, null, null),
            SessionActive: true);

        DiplomaDetailsScreenParts screenParts = DiplomaDetailsAssembler.BuildScreenParts(context);

        Assert.False(screenParts.Actions.CanAdmit);
        Assert.NotNull(screenParts.Actions.AdmitBlockedReason);
        Assert.NotEmpty(screenParts.WorkflowProgress.Steps);
    }

    [Fact]
    public void BuildReadOnlyScreenParts_DisablesAllSecretaryActions()
    {
        var diplomaId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();

        DiplomaTopicVersion topic = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diplomaId,
            VersionNumber = 1,
            Title = "Тема",
            Status = TopicVersionStatus.Approved,
            SubmittedAt = DateTimeOffset.UtcNow,
            SupervisorReviewedAt = DateTimeOffset.UtcNow,
            SupervisorReviewedById = supervisorId,
            ReviewedAt = DateTimeOffset.UtcNow,
        };

        Diploma diploma = new()
        {
            Id = diplomaId,
            StudentId = Guid.NewGuid(),
            DefenceSessionId = Guid.NewGuid(),
            SupervisorId = supervisorId,
            DefenceSession = new DefenceSession
            {
                Id = Guid.NewGuid(),
                Year = 2026,
                Type = DefenceSessionType.Bachelor,
                Status = DefenceSessionStatus.Active,
            },
            LifecycleStatus = DiplomaLifecycleStatus.ReviewerAssigned,
            TopicVersions = [topic],
            AdmissionStepAttempts = [],
        };

        ApplicationUser supervisor = new()
        {
            Id = supervisorId,
            FullName = "Supervisor One",
        };

        DiplomaDetailsContext context = new(
            diploma,
            new Dictionary<Guid, ApplicationUser> { [supervisorId] = supervisor },
            Student: new ApplicationUser { Id = diploma.StudentId, FullName = "Student One" },
            StudyGroupName: "КН-41",
            SupervisorName: "Supervisor One",
            ReviewerName: null,
            Comments: [],
            CommentAuthorNames: [],
            EmployeePool: [new PersonOptionDto(Guid.NewGuid(), "Employee One", "employee@test.local")],
            Documents: new DiplomaDocumentsBundleDto([], null, null, null),
            SessionActive: true);

        DiplomaDetailsScreenParts screenParts = DiplomaDetailsAssembler.BuildReadOnlyScreenParts(context);

        Assert.False(screenParts.Actions.ShowOverrideSupervisorSection);
        Assert.False(screenParts.Actions.CanOverrideSupervisor);
        Assert.False(screenParts.Actions.ShowAssignReviewerSection);
        Assert.False(screenParts.Actions.CanAssignReviewer);
        Assert.False(screenParts.Actions.ShowAdmitSection);
        Assert.False(screenParts.Actions.CanAdmit);
        Assert.False(screenParts.Actions.ShowOverrideAdmissionStepSection);
        Assert.False(screenParts.Actions.CanOverrideAdmissionStep);
        Assert.False(screenParts.Actions.ShowAddCommentSection);
        Assert.False(screenParts.Actions.CanAddComment);
        Assert.NotEmpty(screenParts.WorkflowProgress.Steps);
    }
}
