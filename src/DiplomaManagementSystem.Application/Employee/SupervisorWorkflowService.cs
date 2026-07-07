using DiplomaManagementSystem.Application.Authorization;
using DiplomaManagementSystem.Application.Authorization.Contracts;
using DiplomaManagementSystem.Application.Common.Contracts;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Application.Employee;

internal sealed class SupervisorWorkflowService(
    IApplicationDbContext dbContext,
    IDiplomaQueries diplomaQueries,
    ITopicVersionQueries topicVersionQueries,
    IAdmissionStepQueries admissionStepQueries,
    IUserDisplayQueries userDisplayQueries,
    IArchiveGuard archiveGuard,
    IDiplomaAuthorizationService diplomaAuthorizationService,
    IEmployeeWorkloadLimitService employeeWorkloadLimitService,
    SupervisorConfirmationService supervisorConfirmationService,
    TopicReviewService topicReviewService,
    DiplomaLifecycleService diplomaLifecycleService) : ISupervisorWorkflowService
{
    public async Task<IReadOnlyList<PendingStudentDto>> GetPendingStudentsAsync(
        Guid supervisorId,
        CancellationToken cancellationToken = default)
    {
        List<Diploma> diplomas = await diplomaQueries.ListPendingSupervisorStudentsAsync(
            supervisorId,
            cancellationToken);

        return await EmployeeDiplomaListProjection.MapPendingStudentsAsync(
            userDisplayQueries,
            diplomas,
            cancellationToken);
    }

    public async Task ConfirmStudentAsync(
        Guid supervisorId,
        Guid diplomaId,
        CancellationToken cancellationToken = default)
    {
        await diplomaAuthorizationService.EnsureCanPerformAsync(
            supervisorId,
            diplomaId,
            DiplomaAction.ConfirmSupervisor,
            cancellationToken);

        Diploma diploma = await RequireWritableDiplomaAsync(diplomaId, cancellationToken);
        DefenceSession session = diploma.DefenceSession;

        await employeeWorkloadLimitService.EnsureCanAssignSupervisorAsync(
            session.Id,
            supervisorId,
            diploma.Id,
            cancellationToken);

        supervisorConfirmationService.Confirm(diploma, session, supervisorId);
        await DiplomaLifecycleHelper.RecalculateAsync(
            admissionStepQueries,
            topicVersionQueries,
            diplomaLifecycleService,
            diploma,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectStudentAsync(
        Guid supervisorId,
        SupervisorActionDto request,
        CancellationToken cancellationToken = default)
    {
        await diplomaAuthorizationService.EnsureCanPerformAsync(
            supervisorId,
            request.DiplomaId,
            DiplomaAction.RejectSupervisor,
            cancellationToken);

        Diploma diploma = await RequireWritableDiplomaAsync(request.DiplomaId, cancellationToken);
        DefenceSession session = diploma.DefenceSession;

        supervisorConfirmationService.Reject(diploma, session, supervisorId);

        if (!string.IsNullOrWhiteSpace(request.Comment))
        {
            dbContext.DiplomaComments.Add(new DiplomaComment
            {
                Id = Guid.NewGuid(),
                DiplomaId = diploma.Id,
                AuthorId = supervisorId,
                Body = request.Comment.Trim(),
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        await DiplomaLifecycleHelper.RecalculateAsync(
            admissionStepQueries,
            topicVersionQueries,
            diplomaLifecycleService,
            diploma,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TopicReviewItemDto>> GetTopicReviewsAsync(
        Guid supervisorId,
        CancellationToken cancellationToken = default)
    {
        List<DiplomaTopicVersion> versions = await topicVersionQueries.ListPendingSupervisorReviewAsync(
            supervisorId,
            cancellationToken);

        return await EmployeeDiplomaListProjection.MapTopicReviewItemsAsync(
            userDisplayQueries,
            versions,
            cancellationToken);
    }

    public async Task ApproveTopicAsync(
        Guid supervisorId,
        ApproveTopicDto request,
        CancellationToken cancellationToken = default)
    {
        await diplomaAuthorizationService.EnsureCanPerformOnTopicVersionAsync(
            supervisorId,
            request.VersionId,
            DiplomaAction.ApproveTopicAsSupervisor,
            cancellationToken);

        DiplomaTopicVersion version = await RequireWritableTopicVersionAsync(
            request.VersionId,
            TopicVersionStatus.PendingSupervisor,
            cancellationToken);

        topicReviewService.SupervisorApprove(version, supervisorId);
        AddApprovalCommentIfPresent(version.Diploma, supervisorId, request.Comment);
        await DiplomaLifecycleHelper.RecalculateAsync(
            admissionStepQueries,
            topicVersionQueries,
            diplomaLifecycleService,
            version.Diploma,
            cancellationToken);
        version.Diploma.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectTopicAsync(
        Guid supervisorId,
        ReviewTopicDto request,
        CancellationToken cancellationToken = default)
    {
        await diplomaAuthorizationService.EnsureCanPerformOnTopicVersionAsync(
            supervisorId,
            request.VersionId,
            DiplomaAction.RejectTopicAsSupervisor,
            cancellationToken);

        DiplomaTopicVersion version = await RequireWritableTopicVersionAsync(
            request.VersionId,
            TopicVersionStatus.PendingSupervisor,
            cancellationToken);

        topicReviewService.SupervisorReject(version, supervisorId, request.RejectionReason!);
        await DiplomaLifecycleHelper.RecalculateAsync(
            admissionStepQueries,
            topicVersionQueries,
            diplomaLifecycleService,
            version.Diploma,
            cancellationToken);
        version.Diploma.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Diploma> RequireWritableDiplomaAsync(Guid diplomaId, CancellationToken cancellationToken)
    {
        Diploma? diploma = await diplomaQueries.FindWritableAsync(
            new DiplomaWritableCriteria(diplomaId),
            cancellationToken);

        if (diploma is null)
        {
            throw new DomainException(AuthorizationMessages.DiplomaNotFound);
        }

        archiveGuard.EnsureWritable(diploma.DefenceSession);
        return diploma;
    }

    private async Task<DiplomaTopicVersion> RequireWritableTopicVersionAsync(
        Guid versionId,
        TopicVersionStatus expectedStatus,
        CancellationToken cancellationToken)
    {
        DiplomaTopicVersion? version = await topicVersionQueries.FindWritableAsync(versionId, cancellationToken);

        if (version is null)
        {
            throw new DomainException(AuthorizationMessages.TopicVersionNotFound);
        }

        archiveGuard.EnsureWritable(version.Diploma.DefenceSession);

        if (version.Status != expectedStatus)
        {
            throw new DomainException(AuthorizationMessages.TopicVersionWrongState);
        }

        return version;
    }

    private void AddApprovalCommentIfPresent(Diploma diploma, Guid authorId, string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            return;
        }

        dbContext.DiplomaComments.Add(new DiplomaComment
        {
            Id = Guid.NewGuid(),
            DiplomaId = diploma.Id,
            AuthorId = authorId,
            Body = comment.Trim(),
            CreatedAt = DateTimeOffset.UtcNow,
        });
    }
}
