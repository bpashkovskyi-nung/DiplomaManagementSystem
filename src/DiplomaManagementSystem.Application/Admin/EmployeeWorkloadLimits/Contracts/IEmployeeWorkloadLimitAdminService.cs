using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;

namespace DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Contracts;

public interface IEmployeeWorkloadLimitAdminService
{
    Task<EmployeeWorkloadLimitsPageDto?> GetPageAsync(Guid defenceSessionId, CancellationToken cancellationToken = default);

    Task SetLimitAsync(SetEmployeeWorkloadLimitDto request, CancellationToken cancellationToken = default);
}
