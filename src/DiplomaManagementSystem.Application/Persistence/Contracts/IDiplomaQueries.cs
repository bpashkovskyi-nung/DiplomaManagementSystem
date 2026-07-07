using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Persistence.Contracts;

public interface IDiplomaQueries
{
    Task<Diploma?> FindWritableAsync(DiplomaWritableCriteria criteria, CancellationToken cancellationToken = default);

    Task<Diploma?> FindForAuthorizationAsync(Guid diplomaId, CancellationToken cancellationToken = default);

    Task<Diploma?> FindDetailsReadAsync(Guid sessionId, Guid diplomaId, CancellationToken cancellationToken = default);

    Task<Diploma?> FindLatestForStudentReadAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<List<Diploma>> ListPendingCheckpointsByStepAsync(
        AdmissionStep step,
        Func<IQueryable<Diploma>, IQueryable<Diploma>> filter,
        CancellationToken cancellationToken = default);

    Task<List<Diploma>> ListForSessionReadAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<List<Diploma>> ListAdmittedForSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<List<DiplomaDashboardState>> ListDashboardStatesForSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<List<Diploma>> ListReviewerQueueAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default);

    Task<List<Diploma>> ListPendingSupervisorStudentsAsync(
        Guid supervisorId,
        CancellationToken cancellationToken = default);

    Task<List<Diploma>> ListForSupervisorReadAsync(
        Guid supervisorId,
        CancellationToken cancellationToken = default);

    Task<Diploma?> FindForSupervisorReadAsync(
        Guid supervisorId,
        Guid diplomaId,
        CancellationToken cancellationToken = default);

    Task<List<Diploma>> ListForReviewerReadAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default);

    Task<bool> HasApprovedTopicAsync(Guid diplomaId, CancellationToken cancellationToken = default);
}
