using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests.Secretary;

public sealed class SecretaryDiplomaListProjectionTests
{
    [Fact]
    public void MapListItem_WhenStudentMissing_UsesPlaceholderLabels()
    {
        var diplomaId = Guid.NewGuid();
        Diploma diploma = new()
        {
            Id = diplomaId,
            StudentId = Guid.NewGuid(),
            LifecycleStatus = DiplomaLifecycleStatus.AwaitingSupervisor,
            AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
            TopicVersions = [],
            AdmissionStepAttempts = [],
        };

        DiplomaListItemDto item = SecretaryDiplomaListProjection.MapListItem(
            diploma,
            users: new Dictionary<Guid, ApplicationUser>(),
            studyGroupNames: new Dictionary<Guid, string>());

        Assert.Equal("—", item.StudentFullName);
        Assert.Equal(string.Empty, item.StudentEmail);
        Assert.Equal("—", item.StudyGroupName);
        Assert.Null(item.SupervisorName);
        Assert.Equal(0, item.OutcomeStepsCompleted);
    }

    [Fact]
    public void MapListItem_MapsSupervisorTopicAndCompletedSteps()
    {
        var diplomaId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        var groupId = Guid.NewGuid();

        ApplicationUser student = new()
        {
            Id = studentId,
            FullName = "Анна Студент",
            Email = "anna@test.local",
            StudyGroupId = groupId,
        };

        ApplicationUser supervisor = new()
        {
            Id = supervisorId,
            FullName = "Іван Керівник",
            Email = "ivan@test.local",
        };

        Diploma diploma = new()
        {
            Id = diplomaId,
            StudentId = studentId,
            SupervisorId = supervisorId,
            LifecycleStatus = DiplomaLifecycleStatus.DocumentsInProgress,
            AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
            CurrentAdmissionStep = AdmissionStep.FormattingReview,
            TopicVersions =
            [
                new DiplomaTopicVersion
                {
                    DiplomaId = diplomaId,
                    VersionNumber = 2,
                    Title = "Нова тема",
                    Status = TopicVersionStatus.Approved,
                },
                new DiplomaTopicVersion
                {
                    DiplomaId = diplomaId,
                    VersionNumber = 1,
                    Title = "Стара тема",
                    Status = TopicVersionStatus.Rejected,
                },
            ],
            AdmissionStepAttempts =
            [
                new DiplomaAdmissionStepAttempt
                {
                    DiplomaId = diplomaId,
                    Step = AdmissionStep.SupervisorFeedback,
                    AttemptNumber = 1,
                    Outcome = CheckpointOutcome.Approved,
                },
            ],
        };

        DiplomaListItemDto item = SecretaryDiplomaListProjection.MapListItem(
            diploma,
            new Dictionary<Guid, ApplicationUser>
            {
                [studentId] = student,
                [supervisorId] = supervisor,
            },
            new Dictionary<Guid, string> { [groupId] = "КН-41" });

        Assert.Equal("Анна Студент", item.StudentFullName);
        Assert.Equal("anna@test.local", item.StudentEmail);
        Assert.Equal("КН-41", item.StudyGroupName);
        Assert.Equal("Іван Керівник", item.SupervisorName);
        Assert.Equal("Нова тема", item.TopicTitle);
        Assert.Equal(1, item.OutcomeStepsCompleted);
        Assert.Equal(4, item.OutcomeStepsTotal);
    }

    [Fact]
    public void MapListItems_SortsBySurnameThenFullName()
    {
        var groupId = Guid.NewGuid();
        ApplicationUser first = new() { Id = Guid.NewGuid(), FullName = "Євген Куцук", Email = "a@test.local", StudyGroupId = groupId };
        ApplicationUser second = new() { Id = Guid.NewGuid(), FullName = "Анастасія Мокрецька", Email = "b@test.local", StudyGroupId = groupId };
        ApplicationUser third = new() { Id = Guid.NewGuid(), FullName = "Богдан Агафонкін", Email = "c@test.local", StudyGroupId = groupId };

        List<Diploma> diplomas =
        [
            CreateDiplomaForStudent(first.Id),
            CreateDiplomaForStudent(second.Id),
            CreateDiplomaForStudent(third.Id),
        ];

        List<DiplomaListItemDto> items = SecretaryDiplomaListProjection.MapListItems(
            diplomas,
            new Dictionary<Guid, ApplicationUser>
            {
                [first.Id] = first,
                [second.Id] = second,
                [third.Id] = third,
            },
            new Dictionary<Guid, string> { [groupId] = "КН-41" });

        Assert.Equal(
            ["Богдан Агафонкін", "Євген Куцук", "Анастасія Мокрецька"],
            items.Select(item => item.StudentFullName).ToList());
    }

    private static Diploma CreateDiplomaForStudent(Guid studentId) => new()
    {
        Id = Guid.NewGuid(),
        StudentId = studentId,
        LifecycleStatus = DiplomaLifecycleStatus.AwaitingSupervisor,
        AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
        TopicVersions = [],
        AdmissionStepAttempts = [],
    };
}
