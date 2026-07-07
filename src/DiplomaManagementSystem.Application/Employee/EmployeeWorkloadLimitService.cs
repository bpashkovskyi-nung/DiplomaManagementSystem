using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain;

namespace DiplomaManagementSystem.Application.Employee;

internal sealed class EmployeeWorkloadLimitService(IEmployeeWorkloadLimitQueries queries) : IEmployeeWorkloadLimitService
{
    public async Task EnsureCanAssignSupervisorAsync(
        Guid defenceSessionId,
        Guid supervisorId,
        Guid diplomaId,
        CancellationToken cancellationToken = default)
    {
        int? limit = await queries.GetSupervisorLimitAsync(defenceSessionId, supervisorId, cancellationToken);
        if (!limit.HasValue)
        {
            return;
        }

        int currentCount = await queries.CountConfirmedSupervisorStudentsAsync(
            defenceSessionId,
            supervisorId,
            diplomaId,
            cancellationToken);

        EmployeeWorkloadLimitPolicy.EnsureCanTakeSupervisorSlot(currentCount, limit);
    }

    public async Task EnsureCanAssignReviewerAsync(
        Guid defenceSessionId,
        Guid reviewerId,
        Guid diplomaId,
        CancellationToken cancellationToken = default)
    {
        int? limit = await queries.GetReviewerLimitAsync(defenceSessionId, reviewerId, cancellationToken);
        if (!limit.HasValue)
        {
            return;
        }

        int currentCount = await queries.CountReviewerAssignmentsAsync(
            defenceSessionId,
            reviewerId,
            diplomaId,
            cancellationToken);

        EmployeeWorkloadLimitPolicy.EnsureCanTakeReviewerSlot(currentCount, limit);
    }
}
