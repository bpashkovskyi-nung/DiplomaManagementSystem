namespace DiplomaManagementSystem.Application.Employee.Contracts;

public interface IEmployeeWorkloadLimitService
{
    Task EnsureCanAssignSupervisorAsync(
        Guid defenceSessionId,
        Guid supervisorId,
        Guid diplomaId,
        CancellationToken cancellationToken = default);

    Task EnsureCanAssignReviewerAsync(
        Guid defenceSessionId,
        Guid reviewerId,
        Guid diplomaId,
        CancellationToken cancellationToken = default);
}
