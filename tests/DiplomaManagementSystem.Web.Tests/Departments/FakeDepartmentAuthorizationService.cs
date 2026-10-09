using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Web.Tests.Departments;

internal sealed class FakeDepartmentAuthorizationService : IDepartmentAuthorizationService
{
    private readonly Dictionary<Guid, IReadOnlyList<Guid>> _adminAssignments = new();
    private readonly Dictionary<Guid, IReadOnlyList<Guid>> _employeeMemberships = new();
    private readonly HashSet<Guid> _superAdmins = [];

    public void SetAdminDepartments(Guid userId, params Guid[] departmentIds) =>
        _adminAssignments[userId] = departmentIds;

    public void SetEmployeeDepartments(Guid userId, params Guid[] departmentIds) =>
        _employeeMemberships[userId] = departmentIds;

    public void SetSuperAdmin(Guid userId) => _superAdmins.Add(userId);

    public Task<bool> IsSuperAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_superAdmins.Contains(userId));

    public Task<IReadOnlyList<Guid>> GetAdminDepartmentIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_adminAssignments.GetValueOrDefault(userId, []));

    public Task<IReadOnlyList<Guid>> GetEmployeeDepartmentIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_employeeMemberships.GetValueOrDefault(userId, []));

    public Task EnsureDepartmentAdminAccessAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (!_adminAssignments.GetValueOrDefault(userId, []).Contains(departmentId) && !_superAdmins.Contains(userId))
        {
            throw new DomainException("Access denied.");
        }

        return Task.CompletedTask;
    }

    public Task EnsureDepartmentEmployeeAccessAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (!_employeeMemberships.GetValueOrDefault(userId, []).Contains(departmentId))
        {
            throw new DomainException("Access denied.");
        }

        return Task.CompletedTask;
    }

    public Task EnsureSessionInDepartmentAsync(
        Guid sessionId,
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
