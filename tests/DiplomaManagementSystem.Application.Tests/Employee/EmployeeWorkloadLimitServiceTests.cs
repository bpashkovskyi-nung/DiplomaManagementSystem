using DiplomaManagementSystem.Application.Employee;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Application.Tests.Employee;

public sealed class EmployeeWorkloadLimitServiceTests
{
    [Fact]
    public async Task EnsureCanAssignSupervisorAsync_WhenLimitReached_Throws()
    {
        Guid sessionId = Guid.NewGuid();
        Guid supervisorId = Guid.NewGuid();
        Guid diplomaId = Guid.NewGuid();

        FakeWorkloadLimitQueries queries = new()
        {
            SupervisorLimit = 1,
            ConfirmedSupervisorCount = 1,
        };

        EmployeeWorkloadLimitService service = new(queries);

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanAssignSupervisorAsync(sessionId, supervisorId, diplomaId, CancellationToken.None));

        Assert.Equal(EmployeeWorkloadLimitMessages.SupervisorLimitReached, exception.Message);
        Assert.Equal(diplomaId, queries.LastExcludedSupervisorDiplomaId);
    }

    [Fact]
    public async Task EnsureCanAssignSupervisorAsync_WhenBelowLimit_DoesNotThrow()
    {
        FakeWorkloadLimitQueries queries = new()
        {
            SupervisorLimit = 2,
            ConfirmedSupervisorCount = 1,
        };

        EmployeeWorkloadLimitService service = new(queries);

        await service.EnsureCanAssignSupervisorAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task EnsureCanAssignSupervisorAsync_WhenNoLimit_DoesNotThrow()
    {
        FakeWorkloadLimitQueries queries = new();
        EmployeeWorkloadLimitService service = new(queries);

        await service.EnsureCanAssignSupervisorAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
    }

    [Fact]
    public async Task EnsureCanAssignReviewerAsync_WhenLimitReached_Throws()
    {
        FakeWorkloadLimitQueries queries = new()
        {
            ReviewerLimit = 1,
            ReviewerAssignmentCount = 1,
        };

        EmployeeWorkloadLimitService service = new(queries);

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanAssignReviewerAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(EmployeeWorkloadLimitMessages.ReviewerLimitReached, exception.Message);
    }

    [Fact]
    public async Task EnsureCanAssignReviewerAsync_WhenNoLimit_DoesNotThrow()
    {
        FakeWorkloadLimitQueries queries = new();
        EmployeeWorkloadLimitService service = new(queries);

        await service.EnsureCanAssignReviewerAsync(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);
    }

    private sealed class FakeWorkloadLimitQueries : IEmployeeWorkloadLimitQueries
    {
        public int? SupervisorLimit { get; init; }

        public int? ReviewerLimit { get; init; }

        public int ConfirmedSupervisorCount { get; init; }

        public int ReviewerAssignmentCount { get; init; }

        public Guid? LastExcludedSupervisorDiplomaId { get; private set; }

        public Guid? LastExcludedReviewerDiplomaId { get; private set; }

        public Task<int?> GetSupervisorLimitAsync(
            Guid defenceSessionId,
            Guid employeeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SupervisorLimit);

        public Task<int?> GetReviewerLimitAsync(
            Guid defenceSessionId,
            Guid employeeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(ReviewerLimit);

        public Task<int> CountConfirmedSupervisorStudentsAsync(
            Guid defenceSessionId,
            Guid employeeId,
            Guid? excludeDiplomaId = null,
            CancellationToken cancellationToken = default)
        {
            LastExcludedSupervisorDiplomaId = excludeDiplomaId;
            return Task.FromResult(ConfirmedSupervisorCount);
        }

        public Task<int> CountReviewerAssignmentsAsync(
            Guid defenceSessionId,
            Guid employeeId,
            Guid? excludeDiplomaId = null,
            CancellationToken cancellationToken = default)
        {
            LastExcludedReviewerDiplomaId = excludeDiplomaId;
            return Task.FromResult(ReviewerAssignmentCount);
        }
    }
}
