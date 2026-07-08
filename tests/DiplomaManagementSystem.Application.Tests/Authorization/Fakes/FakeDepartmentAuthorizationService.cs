using DiplomaManagementSystem.Application.Departments.Contracts;

namespace DiplomaManagementSystem.Application.Tests.Authorization.Fakes;

internal sealed class FakeDepartmentAuthorizationService : IDepartmentAuthorizationService
{
    private readonly HashSet<(Guid UserId, Guid DepartmentId)> _employeeMemberships = [];

    public bool AllowAllEmployeeAccess { get; set; } = true;

    public void AddEmployeeMembership(Guid userId, Guid departmentId) =>
        _employeeMemberships.Add((userId, departmentId));

    public Task<bool> IsSuperAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<IReadOnlyList<Guid>> GetAdminDepartmentIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Guid>>([]);

    public Task<IReadOnlyList<Guid>> GetEmployeeDepartmentIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        List<Guid> departmentIds = _employeeMemberships
            .Where(pair => pair.UserId == userId)
            .Select(pair => pair.DepartmentId)
            .ToList();

        return Task.FromResult<IReadOnlyList<Guid>>(departmentIds);
    }

    public Task EnsureDepartmentAdminAccessAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task EnsureDepartmentEmployeeAccessAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (AllowAllEmployeeAccess || _employeeMemberships.Contains((userId, departmentId)))
        {
            return Task.CompletedTask;
        }

        throw new Domain.Exceptions.DomainException(Application.DepartmentMessages.AccessDenied);
    }

    public Task EnsureSessionInDepartmentAsync(
        Guid sessionId,
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
