using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Dtos;

namespace DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Contracts;

public interface IDepartmentAdminAssignmentService
{
    Task<IReadOnlyList<DepartmentAdminListItemDto>> GetByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DepartmentEmployeeOptionDto>> GetAssignableEmployeesAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task AssignAsync(DepartmentAdminAssignDto dto, CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid assignmentId, CancellationToken cancellationToken = default);
}
