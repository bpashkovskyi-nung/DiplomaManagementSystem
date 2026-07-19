using DiplomaManagementSystem.Application.Secretary.Dtos;

namespace DiplomaManagementSystem.Application.Secretary.Contracts;

public interface ISessionSetupService
{
    Task<SessionSetupPageDto?> GetPageAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task SaveMilestonesAsync(
        Guid sessionId,
        SaveMilestonesDto request,
        CancellationToken cancellationToken = default);

    Task SaveDefenceDatesAsync(
        Guid sessionId,
        SaveDefenceDatesDto request,
        CancellationToken cancellationToken = default);
}

public interface IDefenceDatePreferenceQueueService
{
    Task<DefenceDatePreferenceQueueDto?> GetQueueAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);
}
