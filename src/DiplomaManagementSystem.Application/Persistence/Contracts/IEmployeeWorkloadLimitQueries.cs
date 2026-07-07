namespace DiplomaManagementSystem.Application.Persistence.Contracts;

public interface IEmployeeWorkloadLimitQueries
{
    Task<int?> GetSupervisorLimitAsync(
        Guid defenceSessionId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<int?> GetReviewerLimitAsync(
        Guid defenceSessionId,
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<int> CountConfirmedSupervisorStudentsAsync(
        Guid defenceSessionId,
        Guid employeeId,
        Guid? excludeDiplomaId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountReviewerAssignmentsAsync(
        Guid defenceSessionId,
        Guid employeeId,
        Guid? excludeDiplomaId = null,
        CancellationToken cancellationToken = default);
}
