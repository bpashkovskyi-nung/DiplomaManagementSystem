using DiplomaManagementSystem.Application.SuperAdmin.Departments.Dtos;

namespace DiplomaManagementSystem.Application.SuperAdmin.Departments.Contracts;

public interface IDepartmentAdminService
{
    Task<IReadOnlyList<DepartmentListItemDto>> GetAllAsync(
        Guid? facultyId = null,
        CancellationToken cancellationToken = default);

    Task<DepartmentFormDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(DepartmentFormDto form, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, DepartmentFormDto form, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
