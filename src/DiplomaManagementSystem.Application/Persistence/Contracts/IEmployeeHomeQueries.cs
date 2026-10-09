namespace DiplomaManagementSystem.Application.Persistence.Contracts;

public interface IEmployeeHomeQueries
{
    Task<int> CountPendingSupervisorStudentsAsync(Guid supervisorId, CancellationToken cancellationToken = default);

    Task<int> CountPendingSupervisorTopicsAsync(Guid supervisorId, CancellationToken cancellationToken = default);

    Task<bool> HasAnySupervisorDiplomasAsync(
        Guid supervisorId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountPendingHeadTopicsAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken = default);

    Task<int> CountPendingSupervisorFeedbackAsync(Guid supervisorId, CancellationToken cancellationToken = default);

    Task<int> CountPendingReviewerAssignmentsAsync(Guid reviewerId, CancellationToken cancellationToken = default);

    Task<bool> HasAnyReviewerDiplomasAsync(
        Guid reviewerId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountPendingAntiPlagiarismAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken = default);

    Task<int> CountPendingFormattingReviewAsync(IReadOnlyCollection<Guid> sessionIds, CancellationToken cancellationToken = default);
}
