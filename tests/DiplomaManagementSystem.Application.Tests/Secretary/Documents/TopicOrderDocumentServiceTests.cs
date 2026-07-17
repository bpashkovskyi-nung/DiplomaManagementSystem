using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Documents;
using DiplomaManagementSystem.Application.Secretary.Documents.Dtos;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Application.Tests.Secretary.Documents;

public sealed class TopicOrderDocumentServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TopicOrderDocumentService _service;

    public TopicOrderDocumentServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _service = new TopicOrderDocumentService(
            _dbContext,
            new StubDefenceSessionQueries(_dbContext),
            new StubStudyGroupQueries(_dbContext),
            new StubUserDisplayQueries(_dbContext),
            new StubTopicVersionQueries(_dbContext),
            new StubAnnualRoleQueries(_dbContext),
            new TopicOrderDocxGenerator(Microsoft.Extensions.Options.Options.Create(new OrganizationOptions())));
    }

    [Fact]
    public async Task GetFormAsync_WhenSessionExists_ReturnsGroups()
    {
        TopicOrderSeed seed = await SeedSessionWithTwoGroupsAsync(
            specialtyCodeB: "123",
            studyFormA: "очної форми навчання",
            studyFormB: "очної форми навчання");

        TopicOrderFormDto? form = await _service.GetFormAsync(seed.SessionId);

        Assert.NotNull(form);
        Assert.Equal(seed.SessionId, form.SessionId);
        Assert.Equal(2, form.StudyGroups.Count);
    }

    [Fact]
    public async Task BuildPreviewAsync_WhenGroupsMatch_ReturnsPreviewWithWarnings()
    {
        TopicOrderSeed seed = await SeedSessionWithTwoGroupsAsync(
            specialtyCodeB: "123",
            studyFormA: "очної форми навчання",
            studyFormB: "очної форми навчання");

        TopicOrderGenerateRequestDto request = new(
            seed.SessionId,
            "1",
            2026,
            [seed.GroupAId, seed.GroupBId]);

        TopicOrderPreviewDto? preview = await _service.BuildPreviewAsync(request);

        Assert.NotNull(preview);
        Assert.False(preview.CanGenerate);
        Assert.Contains(
            preview.Document.Warnings,
            warning => warning.Contains("затвердженою темою", StringComparison.OrdinalIgnoreCase));
        Assert.Equal("1", preview.Document.OrderNumber);
        Assert.NotEmpty(preview.Document.DepartmentInfo.SpecialtyCode);
    }

    [Fact]
    public async Task BuildPreviewAsync_WhenEligibleDiplomaExists_CanGenerateAndExportDocx()
    {
        TopicOrderSeed seed = await SeedSessionWithTwoGroupsAsync(
            specialtyCodeB: "123",
            studyFormA: "очної форми навчання",
            studyFormB: "очної форми навчання");
        await SeedEligibleDiplomaAsync(seed);

        TopicOrderGenerateRequestDto request = new(
            seed.SessionId,
            "12-A",
            2026,
            [seed.GroupAId]);

        TopicOrderPreviewDto? preview = await _service.BuildPreviewAsync(request);

        Assert.NotNull(preview);
        Assert.True(preview.CanGenerate);
        Assert.Single(preview.Document.Students);
        Assert.Equal("Тестова тема диплома", preview.Document.Students[0].TopicTitle);
        Assert.Contains("Студент", preview.Document.Students[0].StudentFullName, StringComparison.Ordinal);
        Assert.NotNull(preview.Document.FormattingReviewerLine);
        Assert.NotNull(preview.Document.DepartmentHeadLine);
        Assert.Single(preview.Document.Reviewers);

        byte[]? docx = await _service.ExportDocxAsync(request);
        Assert.NotNull(docx);
        Assert.True(docx.Length > 0);
    }

    [Fact]
    public async Task BuildPreviewAsync_WhenGroupsHaveDifferentSpecialties_Throws()
    {
        TopicOrderSeed seed = await SeedSessionWithTwoGroupsAsync(
            specialtyCodeB: "456",
            studyFormA: "очної форми навчання",
            studyFormB: "очної форми навчання");

        TopicOrderGenerateRequestDto request = new(
            seed.SessionId,
            "1",
            2026,
            [seed.GroupAId, seed.GroupBId]);

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            _service.BuildPreviewAsync(request));

        Assert.Contains("спеціальності", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BuildPreviewAsync_WhenGroupsHaveDifferentStudyForms_Throws()
    {
        TopicOrderSeed seed = await SeedSessionWithTwoGroupsAsync(
            specialtyCodeB: "123",
            studyFormA: "очної форми навчання",
            studyFormB: "заочної форми навчання");

        TopicOrderGenerateRequestDto request = new(
            seed.SessionId,
            "1",
            2026,
            [seed.GroupAId, seed.GroupBId]);

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            _service.BuildPreviewAsync(request));

        Assert.Contains("форму навчання", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<TopicOrderSeed> SeedSessionWithTwoGroupsAsync(
        string specialtyCodeB,
        string studyFormA,
        string studyFormB)
    {
        (Guid departmentId, Guid specialtyAId) = await OrganizationTestData.SeedDepartmentWithSpecialtyAsync(
            _dbContext,
            specialtyCode: "123",
            specialtyName: "Спеціальність A");

        Specialty specialtyB = new()
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            Code = specialtyCodeB,
            Name = "Спеціальність B",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _dbContext.Specialties.Add(specialtyB);

        Guid sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Semester = 1,
            Status = DefenceSessionStatus.Active,
            DepartmentId = departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        Guid groupAId = Guid.NewGuid();
        Guid groupBId = Guid.NewGuid();
        _dbContext.StudyGroups.AddRange(
            new StudyGroup
            {
                Id = groupAId,
                Name = "КН-41",
                Course = 4,
                DefenceSessionId = sessionId,
                SpecialtyId = specialtyAId,
                StudyForm = studyFormA,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            new StudyGroup
            {
                Id = groupBId,
                Name = "КН-42",
                Course = 4,
                DefenceSessionId = sessionId,
                SpecialtyId = specialtyCodeB == "123" ? specialtyAId : specialtyB.Id,
                StudyForm = studyFormB,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        await _dbContext.SaveChangesAsync();

        return new TopicOrderSeed(sessionId, groupAId, groupBId);
    }

    private async Task SeedEligibleDiplomaAsync(TopicOrderSeed seed)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ApplicationUser student = CreateUser("Студент Тестовий", "student.topic@test.local", UserKind.Student, seed.GroupAId, seed.SessionId);
        ApplicationUser supervisor = CreateUser("Керівник Тестовий", "supervisor.topic@test.local", UserKind.Employee);
        ApplicationUser reviewer = CreateUser("Рецензент Тестовий", "reviewer.topic@test.local", UserKind.Employee);
        ApplicationUser formatting = CreateUser("Нормоконтролер Тестовий", "formatting.topic@test.local", UserKind.Employee);
        ApplicationUser head = CreateUser("Завідувач Тестовий", "head.topic@test.local", UserKind.Employee);

        Guid diplomaId = Guid.NewGuid();
        _dbContext.Users.AddRange(student, supervisor, reviewer, formatting, head);
        _dbContext.Diplomas.Add(new Diploma
        {
            Id = diplomaId,
            DefenceSessionId = seed.SessionId,
            StudentId = student.Id,
            SupervisorId = supervisor.Id,
            ReviewerId = reviewer.Id,
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Assigned,
            LifecycleStatus = DiplomaLifecycleStatus.TopicApproved,
            AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
            CreatedAt = now,
            UpdatedAt = now,
            TopicVersions =
            [
                new DiplomaTopicVersion
                {
                    Id = Guid.NewGuid(),
                    DiplomaId = diplomaId,
                    VersionNumber = 1,
                    Title = "Тестова тема диплома",
                    Status = TopicVersionStatus.Approved,
                    SubmittedAt = now,
                    ReviewedAt = now,
                    SupervisorReviewedAt = now,
                    SupervisorReviewedById = supervisor.Id,
                },
            ],
        });
        _dbContext.AnnualRoleAssignments.AddRange(
            new AnnualRoleAssignment
            {
                Id = Guid.NewGuid(),
                DefenceSessionId = seed.SessionId,
                EmployeeId = formatting.Id,
                RoleType = AnnualRoleType.FormattingReviewer,
                AssignedAt = now,
            },
            new AnnualRoleAssignment
            {
                Id = Guid.NewGuid(),
                DefenceSessionId = seed.SessionId,
                EmployeeId = head.Id,
                RoleType = AnnualRoleType.DepartmentHead,
                AssignedAt = now,
            });
        await _dbContext.SaveChangesAsync();
    }

    private static ApplicationUser CreateUser(
        string fullName,
        string email,
        UserKind userKind,
        Guid? studyGroupId = null,
        Guid? defenceSessionId = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = fullName,
            UserKind = userKind,
            StudyGroupId = studyGroupId,
            DefenceSessionId = defenceSessionId,
            CreatedAt = DateTimeOffset.UtcNow,
            EmailConfirmed = true,
            AcademicRank = EmployeeAcademicRank.AssociateProfessor,
            ShortDisplayName = AcademicNameFormatter.ToShortDisplayName(fullName),
        };

    public void Dispose() => _dbContext.Dispose();

    private sealed record TopicOrderSeed(Guid SessionId, Guid GroupAId, Guid GroupBId);

    private sealed class StubDefenceSessionQueries(ApplicationDbContext dbContext) : IDefenceSessionQueries
    {
        public Task<bool> ExistsAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            dbContext.DefenceSessions.AnyAsync(session => session.Id == sessionId, cancellationToken);

        public async Task<DefenceSessionType?> GetTypeAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            return await dbContext.DefenceSessions
                .AsNoTracking()
                .Where(session => session.Id == sessionId)
                .Select(session => (DefenceSessionType?)session.Type)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<DefenceSession?> FindReadAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            dbContext.DefenceSessions.AsNoTracking().FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);

        public async Task<DefenceSessionSummary?> FindSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default)
        {
            DefenceSession? session = await FindReadAsync(sessionId, cancellationToken);
            return session is null
                ? null
                : new DefenceSessionSummary(session.Id, session.Year, session.Type, session.Semester);
        }

        public Task<DefenceSessionSummary?> FindForStudentAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<DefenceSessionSummary?>(null);
    }

    private sealed class StubStudyGroupQueries(ApplicationDbContext dbContext) : IStudyGroupQueries
    {
        public async Task<List<StudyGroupOption>> ListOptionsForSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default)
        {
            return await dbContext.StudyGroups
                .AsNoTracking()
                .Where(group => group.DefenceSessionId == sessionId)
                .OrderBy(group => group.Name)
                .Select(group => new StudyGroupOption(group.Id, group.Name, group.Course))
                .ToListAsync(cancellationToken);
        }

        public Task<string?> GetNameAsync(Guid studyGroupId, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class StubUserDisplayQueries(ApplicationDbContext dbContext) : IUserDisplayQueries
    {
        public async Task<Dictionary<Guid, ApplicationUser>> LoadUsersAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default)
        {
            if (userIds.Count == 0)
            {
                return [];
            }

            return await dbContext.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .ToDictionaryAsync(user => user.Id, cancellationToken);
        }

        public Task<Dictionary<Guid, string>> LoadFullNamesAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<Guid, string>());

        public Task<Dictionary<Guid, string>> LoadStudyGroupNamesAsync(
            IReadOnlyCollection<Guid> studyGroupIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<Guid, string>());

        public Task<Dictionary<Guid, StudentDisplayInfo>> LoadStudentDisplaysAsync(
            IReadOnlyCollection<Guid> studentIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<Guid, StudentDisplayInfo>());

        public Task<List<UserOption>> LoadEmployeeOptionsForDepartmentAsync(
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<UserOption>());

        public Task<bool> IsActiveDepartmentEmployeeAsync(
            Guid userId,
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<StudentStorageContext?> GetStudentStorageContextAsync(
            Guid studentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StudentStorageContext?>(null);
    }

    private sealed class StubTopicVersionQueries(ApplicationDbContext dbContext) : ITopicVersionQueries
    {
        public Task<DiplomaTopicVersion?> GetLatestAsync(Guid diplomaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiplomaTopicVersion?>(null);

        public async Task<Dictionary<Guid, string>> GetApprovedTitlesAsync(
            IReadOnlyCollection<Guid> diplomaIds,
            CancellationToken cancellationToken = default)
        {
            if (diplomaIds.Count == 0)
            {
                return [];
            }

            List<DiplomaTopicVersion> versions = await dbContext.DiplomaTopicVersions
                .AsNoTracking()
                .Where(version => diplomaIds.Contains(version.DiplomaId)
                                  && version.Status == TopicVersionStatus.Approved)
                .ToListAsync(cancellationToken);

            return versions
                .GroupBy(version => version.DiplomaId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(version => version.VersionNumber).First().Title);
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

    private sealed class StubAnnualRoleQueries(ApplicationDbContext dbContext) : IAnnualRoleQueries
    {
        public Task<List<Guid>> GetSessionIdsAsync(
            Guid employeeId,
            AnnualRoleType roleType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Guid>());

        public Task<bool> HasRoleForSessionAsync(
            Guid employeeId,
            Guid defenceSessionId,
            AnnualRoleType roleType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> IsSecretaryAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> CanAccessSessionAsSecretaryAsync(
            Guid userId,
            Guid defenceSessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<List<SecretarySessionRow>> ListAccessibleSecretarySessionsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<SecretarySessionRow>());

        public async Task<Guid?> GetEmployeeIdForSessionRoleAsync(
            Guid sessionId,
            AnnualRoleType roleType,
            CancellationToken cancellationToken = default)
        {
            return await dbContext.AnnualRoleAssignments
                .AsNoTracking()
                .Where(assignment => assignment.DefenceSessionId == sessionId && assignment.RoleType == roleType)
                .Select(assignment => (Guid?)assignment.EmployeeId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
