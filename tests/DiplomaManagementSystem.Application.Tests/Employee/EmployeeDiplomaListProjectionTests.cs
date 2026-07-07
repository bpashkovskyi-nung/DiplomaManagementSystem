using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Application.Employee;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests.Employee;

public sealed class EmployeeDiplomaListProjectionTests
{
    [Fact]
    public async Task MapPendingStudentsAsync_WhenEmpty_ReturnsEmptyList()
    {
        FakeUserDisplayQueries queries = new();

        IReadOnlyList<PendingStudentDto> items = await EmployeeDiplomaListProjection.MapPendingStudentsAsync(
            queries,
            [],
            CancellationToken.None);

        Assert.Empty(items);
    }

    [Fact]
    public async Task MapPendingStudentsAsync_WhenDisplayMissing_UsesPlaceholderAndSortsBySurname()
    {
        Guid firstStudentId = Guid.NewGuid();
        Guid secondStudentId = Guid.NewGuid();
        DateTimeOffset updatedAt = DateTimeOffset.UtcNow;

        FakeUserDisplayQueries queries = new()
        {
            StudentDisplays =
            {
                [secondStudentId] = new StudentDisplayInfo("Євген Куцук", "КН-41"),
            },
        };

        List<Diploma> diplomas =
        [
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = firstStudentId,
                UpdatedAt = updatedAt,
            },
            new()
            {
                Id = Guid.NewGuid(),
                StudentId = secondStudentId,
                UpdatedAt = updatedAt,
            },
        ];

        IReadOnlyList<PendingStudentDto> items = await EmployeeDiplomaListProjection.MapPendingStudentsAsync(
            queries,
            diplomas,
            CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal("—", items[0].StudentFullName);
        Assert.Equal("—", items[0].StudyGroupName);
        Assert.Equal("Євген Куцук", items[1].StudentFullName);
        Assert.Equal("КН-41", items[1].StudyGroupName);
    }

    [Fact]
    public async Task MapPendingCheckpointItemsAsync_MapsTopicTitle()
    {
        Guid studentId = Guid.NewGuid();
        Guid diplomaId = Guid.NewGuid();

        FakeUserDisplayQueries userQueries = new()
        {
            StudentDisplays =
            {
                [studentId] = new StudentDisplayInfo("Анна Студент", "КН-42"),
            },
        };

        FakeTopicVersionQueries topicQueries = new()
        {
            ApprovedTitles = { [diplomaId] = "Тема диплома" },
        };

        IReadOnlyList<PendingCheckpointItemDto> items =
            await EmployeeDiplomaListProjection.MapPendingCheckpointItemsAsync(
                userQueries,
                topicQueries,
                [
                    new Diploma
                    {
                        Id = diplomaId,
                        StudentId = studentId,
                    },
                ],
                new Dictionary<Guid, DiplomaDocumentDto>(),
                CancellationToken.None);

        PendingCheckpointItemDto item = Assert.Single(items);
        Assert.Equal("Анна Студент", item.StudentFullName);
        Assert.Equal("КН-42", item.StudyGroupName);
        Assert.Equal("Тема диплома", item.TopicTitle);
    }

    [Fact]
    public async Task MapPendingCheckpointItemsAsync_MapsLatestStudentWork()
    {
        Guid studentId = Guid.NewGuid();
        Guid diplomaId = Guid.NewGuid();

        FakeUserDisplayQueries userQueries = new()
        {
            StudentDisplays =
            {
                [studentId] = new StudentDisplayInfo("Анна Студент", "КН-42"),
            },
        };

        FakeTopicVersionQueries topicQueries = new();
        Dictionary<Guid, DiplomaDocumentDto> latestWork = new()
        {
            [diplomaId] = new DiplomaDocumentDto(
                Guid.NewGuid(),
                DiplomaDocumentKind.StudentWork,
                2,
                "Robota_v2.pdf",
                "/files/view/abc",
                1024,
                DateTimeOffset.UtcNow,
                null),
        };

        IReadOnlyList<PendingCheckpointItemDto> items =
            await EmployeeDiplomaListProjection.MapPendingCheckpointItemsAsync(
                userQueries,
                topicQueries,
                [new Diploma { Id = diplomaId, StudentId = studentId }],
                latestWork,
                CancellationToken.None);

        PendingCheckpointItemDto item = Assert.Single(items);
        Assert.NotNull(item.LatestStudentWork);
        Assert.Equal("Robota_v2.pdf", item.LatestStudentWork!.FileName);
        Assert.Equal("/files/view/abc", item.LatestStudentWork.ViewUrl);
        Assert.Equal(2, item.LatestStudentWork.VersionNumber);
    }

    [Fact]
    public async Task MapTopicReviewItemsAsync_WhenEmpty_ReturnsEmptyList()
    {
        FakeUserDisplayQueries queries = new();

        IReadOnlyList<TopicReviewItemDto> items = await EmployeeDiplomaListProjection.MapTopicReviewItemsAsync(
            queries,
            [],
            CancellationToken.None);

        Assert.Empty(items);
    }

    [Fact]
    public async Task MapTopicReviewItemsAsync_MapsVersionsAndSortsBySurname()
    {
        Guid firstStudentId = Guid.NewGuid();
        Guid secondStudentId = Guid.NewGuid();
        Guid supervisorId = Guid.NewGuid();
        DateTimeOffset submittedAt = DateTimeOffset.UtcNow;

        FakeUserDisplayQueries queries = new()
        {
            FullNames =
            {
                [firstStudentId] = "Євген Куцук",
                [secondStudentId] = "Анастасія Мокрецька",
                [supervisorId] = "Олена Керівник",
            },
        };

        IReadOnlyList<TopicReviewItemDto> items = await EmployeeDiplomaListProjection.MapTopicReviewItemsAsync(
            queries,
            [
                new DiplomaTopicVersion
                {
                    Id = Guid.NewGuid(),
                    DiplomaId = Guid.NewGuid(),
                    Diploma = new Diploma { StudentId = firstStudentId, SupervisorId = supervisorId },
                    Title = "Тема Б",
                    VersionNumber = 1,
                    SubmittedAt = submittedAt,
                },
                new DiplomaTopicVersion
                {
                    Id = Guid.NewGuid(),
                    DiplomaId = Guid.NewGuid(),
                    Diploma = new Diploma { StudentId = secondStudentId },
                    Title = "Тема А",
                    VersionNumber = 2,
                    SubmittedAt = submittedAt,
                },
            ],
            CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal("Євген Куцук", items[0].StudentFullName);
        Assert.Equal("Олена Керівник", items[0].SupervisorFullName);
        Assert.Equal("Тема Б", items[0].Title);
        Assert.Equal("Анастасія Мокрецька", items[1].StudentFullName);
        Assert.Null(items[1].SupervisorFullName);
        Assert.Equal("Тема А", items[1].Title);
        Assert.Equal(2, items[1].VersionNumber);
    }

    [Fact]
    public async Task MapReviewerAssignmentsAsync_MapsStatusAndTopic()
    {
        Guid studentId = Guid.NewGuid();
        Guid diplomaId = Guid.NewGuid();

        FakeUserDisplayQueries userQueries = new()
        {
            StudentDisplays = { [studentId] = new StudentDisplayInfo("Петро Студент", "КН-42") },
        };

        FakeTopicVersionQueries topicQueries = new()
        {
            ApprovedTitles = { [diplomaId] = "Затверджена тема" },
        };

        IReadOnlyList<ReviewerAssignmentItemDto> items =
            await EmployeeDiplomaListProjection.MapReviewerAssignmentsAsync(
                userQueries,
                topicQueries,
                [
                    new Diploma
                    {
                        Id = diplomaId,
                        StudentId = studentId,
                        ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned,
                    },
                ],
                new Dictionary<Guid, DiplomaDocumentDto>(),
                CancellationToken.None);

        ReviewerAssignmentItemDto item = Assert.Single(items);
        Assert.Equal("Петро Студент", item.StudentFullName);
        Assert.Equal("КН-42", item.StudyGroupName);
        Assert.Equal("Затверджена тема", item.TopicTitle);
        Assert.Equal(ReviewAssignmentStatus.Assigned, item.ReviewAssignmentStatus);
    }

    [Fact]
    public async Task MapReviewerAssignmentsAsync_WhenEmpty_ReturnsEmptyList()
    {
        FakeUserDisplayQueries userQueries = new();
        FakeTopicVersionQueries topicQueries = new();

        IReadOnlyList<ReviewerAssignmentItemDto> items =
            await EmployeeDiplomaListProjection.MapReviewerAssignmentsAsync(
                userQueries,
                topicQueries,
                [],
                new Dictionary<Guid, DiplomaDocumentDto>(),
                CancellationToken.None);

        Assert.Empty(items);
    }

    private sealed class FakeUserDisplayQueries : IUserDisplayQueries
    {
        public Dictionary<Guid, StudentDisplayInfo> StudentDisplays { get; } = [];

        public Dictionary<Guid, string> FullNames { get; } = [];

        public Task<Dictionary<Guid, ApplicationUser>> LoadUsersAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<Guid, ApplicationUser>());

        public Task<Dictionary<Guid, string>> LoadFullNamesAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            Dictionary<Guid, string> result = userIds
                .Where(FullNames.ContainsKey)
                .ToDictionary(id => id, id => FullNames[id]);

            return Task.FromResult(result);
        }

        public Task<Dictionary<Guid, string>> LoadStudyGroupNamesAsync(
            IReadOnlyCollection<Guid> studyGroupIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<Guid, string>());

        public Task<Dictionary<Guid, StudentDisplayInfo>> LoadStudentDisplaysAsync(
            IReadOnlyCollection<Guid> studentIds,
            CancellationToken cancellationToken = default)
        {
            Dictionary<Guid, StudentDisplayInfo> result = studentIds
                .Where(StudentDisplays.ContainsKey)
                .ToDictionary(id => id, id => StudentDisplays[id]);

            return Task.FromResult(result);
        }

        public Task<List<UserOption>> LoadEmployeeOptionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<UserOption>());

        public Task<bool> IsEmployeeAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<StudentStorageContext?> GetStudentStorageContextAsync(
            Guid studentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StudentStorageContext?>(null);
    }

    private sealed class FakeTopicVersionQueries : ITopicVersionQueries
    {
        public Dictionary<Guid, string> ApprovedTitles { get; } = [];

        public Task<DiplomaTopicVersion?> GetLatestAsync(Guid diplomaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiplomaTopicVersion?>(null);

        public Task<Dictionary<Guid, string>> GetApprovedTitlesAsync(
            IReadOnlyCollection<Guid> diplomaIds,
            CancellationToken cancellationToken = default)
        {
            Dictionary<Guid, string> result = diplomaIds
                .Where(ApprovedTitles.ContainsKey)
                .ToDictionary(id => id, id => ApprovedTitles[id]);

            return Task.FromResult(result);
        }

        public Task<List<DiplomaTopicVersion>> ListPendingHeadReviewAsync(
            IReadOnlyCollection<Guid> sessionIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DiplomaTopicVersion>());

        public Task<List<DiplomaTopicVersion>> ListPendingSupervisorReviewAsync(
            Guid supervisorId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DiplomaTopicVersion>());

        public Task<DiplomaTopicVersion?> FindWritableAsync(
            Guid versionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<DiplomaTopicVersion?>(null);

        public Task<List<DiplomaTopicVersion>> ListForDiplomaWritableAsync(
            Guid diplomaId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<DiplomaTopicVersion>());
    }
}
