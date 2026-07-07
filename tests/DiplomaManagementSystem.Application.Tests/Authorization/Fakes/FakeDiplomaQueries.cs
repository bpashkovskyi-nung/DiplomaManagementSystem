using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests.Authorization.Fakes;

internal sealed class FakeDiplomaQueries(Diploma? diploma = null) : IDiplomaQueries
{
    public Diploma? Diploma { get; set; } = diploma;

    public Task<Diploma?> FindForAuthorizationAsync(Guid diplomaId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Diploma is not null && Diploma.Id == diplomaId ? Diploma : null);

    public Task<Diploma?> FindWritableAsync(DiplomaWritableCriteria criteria, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Diploma?> FindDetailsReadAsync(Guid sessionId, Guid diplomaId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Diploma?> FindLatestForStudentReadAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<Diploma>> ListPendingCheckpointsByStepAsync(
        AdmissionStep step,
        Func<IQueryable<Diploma>, IQueryable<Diploma>> filter,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<Diploma>> ListForSessionReadAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<Diploma>> ListAdmittedForSessionAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<DiplomaDashboardState>> ListDashboardStatesForSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<Diploma>> ListReviewerQueueAsync(Guid reviewerId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<Diploma>> ListPendingSupervisorStudentsAsync(
        Guid supervisorId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<Diploma>> ListForSupervisorReadAsync(
        Guid supervisorId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Diploma?> FindForSupervisorReadAsync(
        Guid supervisorId,
        Guid diplomaId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<List<Diploma>> ListForReviewerReadAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<bool> HasApprovedTopicAsync(Guid diplomaId, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
