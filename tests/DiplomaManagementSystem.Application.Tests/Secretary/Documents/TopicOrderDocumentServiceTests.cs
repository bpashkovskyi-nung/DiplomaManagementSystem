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
            new StubStudyGroupQueries(),
            new StubUserDisplayQueries(),
            new StubTopicVersionQueries(),
            new StubAnnualRoleQueries(),
            new TopicOrderDocxGenerator(Microsoft.Extensions.Options.Options.Create(new OrganizationOptions())));
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

    private sealed class StubStudyGroupQueries : IStudyGroupQueries
    {
        public Task<List<StudyGroupOption>> ListOptionsForSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<StudyGroupOption>());

        public Task<string?> GetNameAsync(Guid studyGroupId, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(null);
    }

    private sealed class StubUserDisplayQueries : IUserDisplayQueries
    {
        public Task<Dictionary<Guid, ApplicationUser>> LoadUsersAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<Guid, ApplicationUser>());

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

        public Task<List<UserOption>> LoadEmployeeOptionsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<UserOption>());

        public Task<bool> IsEmployeeAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<StudentStorageContext?> GetStudentStorageContextAsync(
            Guid studentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<StudentStorageContext?>(null);
    }

    private sealed class StubTopicVersionQueries : ITopicVersionQueries
    {
        public Task<DiplomaTopicVersion?> GetLatestAsync(Guid diplomaId, CancellationToken cancellationToken = default) =>
            Task.FromResult<DiplomaTopicVersion?>(null);

        public Task<Dictionary<Guid, string>> GetApprovedTitlesAsync(
            IReadOnlyCollection<Guid> diplomaIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Dictionary<Guid, string>());

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

    private sealed class StubAnnualRoleQueries : IAnnualRoleQueries
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

        public Task<Guid?> GetEmployeeIdForSessionRoleAsync(
            Guid sessionId,
            AnnualRoleType roleType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(null);
    }
}
