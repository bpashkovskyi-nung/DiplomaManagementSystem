using DiplomaManagementSystem.Application.Secretary.Dtos;

namespace DiplomaManagementSystem.Application.Secretary.Contracts;

public interface ISecretaryDiplomaActionService
{
    Task AssignReviewerAsync(
        Guid actorId,
        Guid sessionId,
        AssignReviewerDto request,
        CancellationToken cancellationToken = default);

    Task AdmitAsync(
        Guid actorId,
        Guid sessionId,
        AdmitDiplomaDto request,
        CancellationToken cancellationToken = default);

    Task ConfirmDefenceDateAsync(
        Guid actorId,
        Guid sessionId,
        ConfirmDefenceDateDto request,
        CancellationToken cancellationToken = default);

    Task OverrideSupervisorAsync(
        Guid actorId,
        Guid sessionId,
        OverrideSupervisorDto request,
        CancellationToken cancellationToken = default);

    Task AddCommentAsync(
        Guid actorId,
        Guid sessionId,
        AddCommentDto request,
        CancellationToken cancellationToken = default);

    Task OverrideAdmissionStepAsync(
        Guid actorId,
        Guid sessionId,
        OverrideAdmissionStepDto request,
        CancellationToken cancellationToken = default);
}
