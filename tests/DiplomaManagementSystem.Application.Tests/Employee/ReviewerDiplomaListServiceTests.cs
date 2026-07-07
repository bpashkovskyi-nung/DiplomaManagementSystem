using DiplomaManagementSystem.Application.Employee;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests.Employee;

public sealed class ReviewerDiplomaListServiceTests
{
    [Fact]
    public async Task GetListAsync_FiltersByReviewerAssignment_ReturnsMappedItems()
    {
        Guid reviewerId = Guid.NewGuid();
        Guid studentId = Guid.NewGuid();
        Guid supervisorId = Guid.NewGuid();
        Guid studyGroupId = Guid.NewGuid();
        Guid sessionId = Guid.NewGuid();

        Diploma diploma = new()
        {
            Id = Guid.NewGuid(),
            DefenceSessionId = sessionId,
            StudentId = studentId,
            SupervisorId = supervisorId,
            ReviewerId = reviewerId,
            ReviewAssignmentStatus = ReviewAssignmentStatus.Completed,
            LifecycleStatus = DiplomaLifecycleStatus.Admitted,
            DefenceSession = new DefenceSession { Id = sessionId, Status = DefenceSessionStatus.Active },
            TopicVersions =
            [
                new DiplomaTopicVersion
                {
                    Id = Guid.NewGuid(),
                    DiplomaId = Guid.NewGuid(),
                    VersionNumber = 1,
                    Title = "Тема",
                    Status = TopicVersionStatus.Approved,
                },
            ],
        };

        FakeReviewerDiplomaQueries queries = new([diploma]);
        FakeUserDisplayQueries userDisplayQueries = new(
            new Dictionary<Guid, ApplicationUser>
            {
                [studentId] = new ApplicationUser
                {
                    Id = studentId,
                    FullName = "Іваненко Іван",
                    Email = "ivan@example.com",
                    StudyGroupId = studyGroupId,
                },
                [supervisorId] = new ApplicationUser
                {
                    Id = supervisorId,
                    FullName = "Петренко Петро",
                    Email = "petro@example.com",
                },
            },
            new Dictionary<Guid, string> { [studyGroupId] = "КН-41" });

        ReviewerDiplomaListService service = new(queries, userDisplayQueries);

        ReviewerDiplomaListPageDto page = await service.GetListAsync(
            reviewerId,
            new DiplomaListFilterDto(null, null, null, null, null, null),
            CancellationToken.None);

        DiplomaListItemDto item = Assert.Single(page.Items);
        Assert.Equal("Іваненко Іван", item.StudentFullName);
        Assert.Equal("КН-41", item.StudyGroupName);
        Assert.Equal("Петренко Петро", item.SupervisorName);
        Assert.Equal("Тема", item.TopicTitle);
    }

    private sealed class FakeReviewerDiplomaQueries(List<Diploma> diplomas) : IDiplomaQueries
    {
        public Task<List<Diploma>> ListForReviewerReadAsync(
            Guid reviewerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(diplomas.Where(diploma => diploma.ReviewerId == reviewerId).ToList());

        public Task<Diploma?> FindWritableAsync(DiplomaWritableCriteria criteria, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Diploma?> FindForAuthorizationAsync(Guid diplomaId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Diploma?> FindDetailsReadAsync(Guid sessionId, Guid diplomaId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Diploma?> FindLatestForStudentReadAsync(Guid studentId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<Diploma>> ListPendingCheckpointsByStepAsync(
            AdmissionStep step,
            Func<IQueryable<Diploma>, IQueryable<Diploma>> filter,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<Diploma>> ListForSessionReadAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<Diploma>> ListAdmittedForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<DiplomaDashboardState>> ListDashboardStatesForSessionAsync(
            Guid sessionId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<Diploma>> ListReviewerQueueAsync(Guid reviewerId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<Diploma>> ListPendingSupervisorStudentsAsync(
            Guid supervisorId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<Diploma>> ListForSupervisorReadAsync(
            Guid supervisorId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Diploma?> FindForSupervisorReadAsync(
            Guid supervisorId,
            Guid diplomaId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> HasApprovedTopicAsync(Guid diplomaId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeUserDisplayQueries(
        Dictionary<Guid, ApplicationUser> users,
        Dictionary<Guid, string> studyGroupNames) : IUserDisplayQueries
    {
        public Task<Dictionary<Guid, ApplicationUser>> LoadUsersAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(users.Where(pair => userIds.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value));

        public Task<Dictionary<Guid, string>> LoadStudyGroupNamesAsync(
            IReadOnlyCollection<Guid> studyGroupIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(studyGroupNames.Where(pair => studyGroupIds.Contains(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value));

        public Task<Dictionary<Guid, string>> LoadFullNamesAsync(
            IReadOnlyCollection<Guid> userIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Dictionary<Guid, StudentDisplayInfo>> LoadStudentDisplaysAsync(
            IReadOnlyCollection<Guid> studentIds,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<List<UserOption>> LoadEmployeeOptionsAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> IsEmployeeAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<StudentStorageContext?> GetStudentStorageContextAsync(
            Guid studentId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
