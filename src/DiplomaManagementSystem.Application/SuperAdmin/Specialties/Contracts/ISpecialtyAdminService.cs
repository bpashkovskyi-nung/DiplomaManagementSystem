using DiplomaManagementSystem.Application.SuperAdmin.Specialties.Dtos;

namespace DiplomaManagementSystem.Application.SuperAdmin.Specialties.Contracts;

public interface ISpecialtyAdminService
{
    Task<IReadOnlyList<SpecialtyListItemDto>> GetByDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpecialtyOptionDto>> GetActiveOptionsForDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpecialtyOptionDto>> GetActiveOptionsForSessionAsync(
        Guid defenceSessionId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(SpecialtyFormDto form, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid id, SpecialtyFormDto form, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}
