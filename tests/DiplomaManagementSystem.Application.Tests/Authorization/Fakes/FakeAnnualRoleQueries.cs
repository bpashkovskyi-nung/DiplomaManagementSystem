using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests.Authorization.Fakes;

internal sealed class FakeAnnualRoleQueries : IAnnualRoleQueries
{
    public bool CanAccessAsSecretary { get; set; }

    public HashSet<(Guid UserId, Guid SessionId, AnnualRoleType Role)> Roles { get; } = [];

    public Task<List<Guid>> GetSessionIdsAsync(
        Guid employeeId,
        AnnualRoleType roleType,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<bool> HasRoleForSessionAsync(
        Guid employeeId,
        Guid defenceSessionId,
        AnnualRoleType roleType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Roles.Contains((employeeId, defenceSessionId, roleType)));

    public Task<bool> IsSecretaryAsync(Guid userId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<bool> CanAccessSessionAsSecretaryAsync(
        Guid userId,
        Guid defenceSessionId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(CanAccessAsSecretary);

    public Task<List<SecretarySessionRow>> ListAccessibleSecretarySessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Guid?> GetEmployeeIdForSessionRoleAsync(
        Guid defenceSessionId,
        AnnualRoleType roleType,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(null);
}
