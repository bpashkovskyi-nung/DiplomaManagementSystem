using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Secretary;

internal sealed class SecretaryDashboardService(
    IDefenceSessionQueries defenceSessionQueries,
    IDiplomaQueries diplomaQueries) : ISecretaryDashboardService
{
    public async Task<SecretaryDashboardDto?> GetDashboardAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        DefenceSessionSummary? session = await defenceSessionQueries.FindSummaryAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        List<DiplomaDashboardState> diplomas = await diplomaQueries.ListDashboardStatesForSessionAsync(
            sessionId,
            cancellationToken);

        Dictionary<DiplomaLifecycleStatus, int> countsByLifecycle = diplomas
            .GroupBy(diploma => diploma.LifecycleStatus)
            .ToDictionary(group => group.Key, group => group.Count());

        List<SecretaryDashboardBucketDto> buckets = [];

        AddLifecycleBucket(buckets, countsByLifecycle, DiplomaLifecycleStatus.AwaitingSupervisor);
        AddLifecycleBucket(buckets, countsByLifecycle, DiplomaLifecycleStatus.SupervisorConfirmed);
        AddLifecycleBucket(buckets, countsByLifecycle, DiplomaLifecycleStatus.TopicInReview);
        AddLifecycleBucket(buckets, countsByLifecycle, DiplomaLifecycleStatus.TopicApproved);
        AddLifecycleBucket(buckets, countsByLifecycle, DiplomaLifecycleStatus.ReviewerAssigned);

        foreach (AdmissionStep step in Enum.GetValues<AdmissionStep>().OrderBy(step => step))
        {
            int count = diplomas.Count(diploma =>
                diploma.LifecycleStatus == DiplomaLifecycleStatus.DocumentsInProgress
                && diploma.CurrentAdmissionStep == step);
            buckets.Add(new SecretaryDashboardBucketDto(null, step, count));
        }

        AddLifecycleBucket(buckets, countsByLifecycle, DiplomaLifecycleStatus.ReadyForAdmission);
        AddLifecycleBucket(buckets, countsByLifecycle, DiplomaLifecycleStatus.Admitted);

        return new SecretaryDashboardDto(
            session.Id,
            session.Type,
            SecretarySessionLabel.Format(session.Year, session.Type, session.Semester),
            buckets,
            diplomas.Count);
    }

    private static void AddLifecycleBucket(
        List<SecretaryDashboardBucketDto> buckets,
        IReadOnlyDictionary<DiplomaLifecycleStatus, int> countsByLifecycle,
        DiplomaLifecycleStatus status)
    {
        int count = countsByLifecycle.GetValueOrDefault(status, 0);
        buckets.Add(new SecretaryDashboardBucketDto(status, null, count));
    }
}
