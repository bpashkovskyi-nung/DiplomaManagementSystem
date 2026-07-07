using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Persistence.Contracts;

public interface IAnnualRoleQueries
{
    Task<List<Guid>> GetSessionIdsAsync(
        Guid employeeId,
        AnnualRoleType roleType,
        CancellationToken cancellationToken = default);

    Task<bool> HasRoleForSessionAsync(
        Guid employeeId,
        Guid defenceSessionId,
        AnnualRoleType roleType,
        CancellationToken cancellationToken = default);

    Task<bool> IsSecretaryAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> CanAccessSessionAsSecretaryAsync(
        Guid userId,
        Guid defenceSessionId,
        CancellationToken cancellationToken = default);

    Task<List<SecretarySessionRow>> ListAccessibleSecretarySessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetEmployeeIdForSessionRoleAsync(
        Guid defenceSessionId,
        AnnualRoleType roleType,
        CancellationToken cancellationToken = default);
}
