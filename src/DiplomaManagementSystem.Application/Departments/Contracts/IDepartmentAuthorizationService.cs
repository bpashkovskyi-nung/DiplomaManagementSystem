namespace DiplomaManagementSystem.Application.Departments.Contracts;

public interface IDepartmentAuthorizationService
{
    Task<bool> IsSuperAdminAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetAdminDepartmentIdsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetEmployeeDepartmentIdsAsync(Guid userId, CancellationToken cancellationToken = default);

    Task EnsureDepartmentAdminAccessAsync(Guid userId, Guid departmentId, CancellationToken cancellationToken = default);

    Task EnsureDepartmentEmployeeAccessAsync(Guid userId, Guid departmentId, CancellationToken cancellationToken = default);

    Task EnsureSessionInDepartmentAsync(Guid sessionId, Guid departmentId, CancellationToken cancellationToken = default);
}
