using DiplomaManagementSystem.Application.Authorization;
using DiplomaManagementSystem.Application.Authorization.Contracts;
using DiplomaManagementSystem.Application.Common.Contracts;
using DiplomaManagementSystem.Application.Documents;
using DiplomaManagementSystem.Application.Documents.Contracts;
using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Employee;

internal sealed class AdmissionReviewService(
    IApplicationDbContext dbContext,
    IArchiveGuard archiveGuard,
    IDiplomaAuthorizationService diplomaAuthorizationService,
    IDiplomaQueries diplomaQueries,
    IAnnualRoleQueries annualRoleQueries,
    IUserDisplayQueries userDisplayQueries,
    ITopicVersionQueries topicVersionQueries,
    IAdmissionStepQueries admissionStepQueries,
    AdmissionWorkflowService admissionWorkflowService,
    DiplomaLifecycleService diplomaLifecycleService,
    IDiplomaDocumentService diplomaDocumentService) : IAdmissionReviewService
{
    public Task<IReadOnlyList<PendingCheckpointItemDto>> GetSupervisorFeedbackPendingAsync(
        Guid supervisorId,
        CancellationToken cancellationToken = default)
    {
        return GetPendingByStepAsync(
            AdmissionStep.SupervisorFeedback,
            query => query.Where(diploma => diploma.SupervisorId == supervisorId),
            cancellationToken);
    }

    public Task CompleteSupervisorFeedbackAsync(
        Guid supervisorId,
        CompleteCheckpointDto request,
        UploadFileContent? document,
        CancellationToken cancellationToken = default) =>
        CompleteCheckpointWithDocumentAsync(
            supervisorId,
            request,
            document,
            AdmissionStep.SupervisorFeedback,
            DiplomaAction.CompleteSupervisorCheckpoint,
            cancellationToken);

    public async Task<IReadOnlyList<ReviewerAssignmentItemDto>> GetReviewerAssignmentsAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default)
    {
        List<Diploma> diplomas = await diplomaQueries.ListReviewerQueueAsync(reviewerId, cancellationToken);

        List<Diploma> pending = diplomas
            .Where(diploma => AdmissionStepStatusResolver.IsStepActionable(
                AdmissionStep.ExternalReview,
                diploma.AdmissionStepAttempts))
            .ToList();

        IReadOnlyDictionary<Guid, DiplomaDocumentDto> latestStudentWork =
            await diplomaDocumentService.GetLatestStudentWorkByDiplomaIdsAsync(
                pending.Select(diploma => diploma.Id).ToList(),
                cancellationToken);

        return await EmployeeDiplomaListProjection.MapReviewerAssignmentsAsync(
            userDisplayQueries,
            topicVersionQueries,
            pending,
            latestStudentWork,
            cancellationToken);
    }

    public Task CompleteExternalReviewAsync(
        Guid reviewerId,
        CompleteCheckpointDto request,
        UploadFileContent? document,
        CancellationToken cancellationToken = default) =>
        CompleteCheckpointWithDocumentAsync(
            reviewerId,
            request,
            document,
            AdmissionStep.ExternalReview,
            DiplomaAction.CompleteExternalReview,
            cancellationToken);

    public async Task<IReadOnlyList<PendingCheckpointItemDto>> GetAntiPlagiarismPendingAsync(
        Guid officerId,
        CancellationToken cancellationToken = default)
    {
        List<Guid> sessionIds = await annualRoleQueries.GetSessionIdsAsync(
            officerId,
            AnnualRoleType.AntiPlagiarismOfficer,
            cancellationToken);

        if (sessionIds.Count == 0)
        {
            return [];
        }

        return await GetPendingByStepAsync(
            AdmissionStep.AntiPlagiarismClearance,
            query => query.Where(diploma => sessionIds.Contains(diploma.DefenceSessionId)),
            cancellationToken);
    }

    public Task CompleteAntiPlagiarismAsync(
        Guid officerId,
        CompleteCheckpointDto request,
        UploadFileContent? document,
        CancellationToken cancellationToken = default) =>
        CompleteCheckpointWithDocumentAsync(
            officerId,
            request,
            document,
            AdmissionStep.AntiPlagiarismClearance,
            DiplomaAction.CompleteAntiPlagiarism,
            cancellationToken);

    public async Task<IReadOnlyList<PendingCheckpointItemDto>> GetFormattingReviewPendingAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default)
    {
        List<Guid> sessionIds = await annualRoleQueries.GetSessionIdsAsync(
            reviewerId,
            AnnualRoleType.FormattingReviewer,
            cancellationToken);

        if (sessionIds.Count == 0)
        {
            return [];
        }

        return await GetPendingByStepAsync(
            AdmissionStep.FormattingReview,
            query => query.Where(diploma => sessionIds.Contains(diploma.DefenceSessionId)),
            cancellationToken);
    }

    public async Task CompleteFormattingReviewAsync(
        Guid reviewerId,
        CompleteCheckpointDto request,
        CancellationToken cancellationToken = default)
    {
        Diploma diploma = await RequireWritableDiplomaAsync(request.DiplomaId, cancellationToken);

        await diplomaAuthorizationService.EnsureCanPerformAsync(
            reviewerId,
            request.DiplomaId,
            DiplomaAction.CompleteFormattingReview,
            cancellationToken);

        DiplomaAdmissionStepAttempt attempt = admissionWorkflowService.RecordAttempt(
            diploma,
            AdmissionStep.FormattingReview,
            diploma.AdmissionStepAttempts,
            reviewerId,
            request.Outcome,
            request.Comment);

        dbContext.DiplomaAdmissionStepAttempts.Add(attempt);

        await DiplomaLifecycleHelper.RecalculateAsync(
            admissionStepQueries,
            topicVersionQueries,
            diplomaLifecycleService,
            diploma,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task CompleteCheckpointWithDocumentAsync(
        Guid actorId,
        CompleteCheckpointDto request,
        UploadFileContent? document,
        AdmissionStep step,
        DiplomaAction action,
        CancellationToken cancellationToken)
    {
        if (CheckpointOutcomeRules.RequiresDocument(request.Outcome)
            && DiplomaDocumentNaming.RequiresFile(step)
            && document is null)
        {
            throw new DomainException("Обов'язково додайте файл (PDF, DOCX або ODT).");
        }

        Diploma diploma = await RequireWritableDiplomaAsync(request.DiplomaId, cancellationToken);

        await diplomaAuthorizationService.EnsureCanPerformAsync(
            actorId,
            request.DiplomaId,
            action,
            cancellationToken);

        DiplomaAdmissionStepAttempt attempt = admissionWorkflowService.RecordAttempt(
            diploma,
            step,
            diploma.AdmissionStepAttempts,
            actorId,
            request.Outcome,
            request.Comment);

        dbContext.DiplomaAdmissionStepAttempts.Add(attempt);

        await DiplomaLifecycleHelper.RecalculateAsync(
            admissionStepQueries,
            topicVersionQueries,
            diplomaLifecycleService,
            diploma,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (document is not null)
        {
            await diplomaDocumentService.UploadCheckpointDocumentAsync(
                actorId,
                request.DiplomaId,
                step,
                attempt.Id,
                document,
                cancellationToken);
        }
    }

    private async Task<IReadOnlyList<PendingCheckpointItemDto>> GetPendingByStepAsync(
        AdmissionStep step,
        Func<IQueryable<Diploma>, IQueryable<Diploma>> filter,
        CancellationToken cancellationToken)
    {
        List<Diploma> diplomas = await diplomaQueries.ListPendingCheckpointsByStepAsync(
            step,
            filter,
            cancellationToken);

        diplomas = diplomas
            .Where(diploma => AdmissionStepStatusResolver.IsStepActionable(step, diploma.AdmissionStepAttempts))
            .ToList();

        IReadOnlyDictionary<Guid, DiplomaDocumentDto> latestStudentWork =
            await diplomaDocumentService.GetLatestStudentWorkByDiplomaIdsAsync(
                diplomas.Select(diploma => diploma.Id).ToList(),
                cancellationToken);

        return await EmployeeDiplomaListProjection.MapPendingCheckpointItemsAsync(
            userDisplayQueries,
            topicVersionQueries,
            diplomas,
            latestStudentWork,
            cancellationToken);
    }

    private async Task<Diploma> RequireWritableDiplomaAsync(Guid diplomaId, CancellationToken cancellationToken)
    {
        Diploma? diploma = await diplomaQueries.FindWritableAsync(
            new DiplomaWritableCriteria(diplomaId, IncludeAdmissionAttempts: true),
            cancellationToken);

        if (diploma is null)
        {
            throw new DomainException(AuthorizationMessages.DiplomaNotFound);
        }

        await ((DbContext)dbContext).Entry(diploma)
            .Collection(d => d.AdmissionStepAttempts)
            .LoadAsync(cancellationToken);

        archiveGuard.EnsureWritable(diploma.DefenceSession);
        return diploma;
    }
}
