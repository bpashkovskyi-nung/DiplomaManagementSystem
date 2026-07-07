using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Storage;

namespace DiplomaManagementSystem.Application.Employee.Contracts;

public interface IAdmissionReviewService
{
    Task<IReadOnlyList<PendingCheckpointItemDto>> GetSupervisorFeedbackPendingAsync(
        Guid supervisorId,
        CancellationToken cancellationToken = default);

    Task CompleteSupervisorFeedbackAsync(
        Guid supervisorId,
        CompleteCheckpointDto request,
        UploadFileContent? document,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReviewerAssignmentItemDto>> GetReviewerAssignmentsAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default);

    Task CompleteExternalReviewAsync(
        Guid reviewerId,
        CompleteCheckpointDto request,
        UploadFileContent? document,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingCheckpointItemDto>> GetAntiPlagiarismPendingAsync(
        Guid officerId,
        CancellationToken cancellationToken = default);

    Task CompleteAntiPlagiarismAsync(
        Guid officerId,
        CompleteCheckpointDto request,
        UploadFileContent? document,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingCheckpointItemDto>> GetFormattingReviewPendingAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default);

    Task CompleteFormattingReviewAsync(
        Guid reviewerId,
        CompleteCheckpointDto request,
        CancellationToken cancellationToken = default);
}
