using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;

namespace DiplomaManagementSystem.Application.SuperAdmin.Faculties.Contracts;

public interface IFacultyAdminService
{
    Task<IReadOnlyList<FacultyListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<FacultyFormDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(FacultyFormDto form, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, FacultyFormDto form, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
