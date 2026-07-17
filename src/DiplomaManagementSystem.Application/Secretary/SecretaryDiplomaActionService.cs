using DiplomaManagementSystem.Application.Audit.Contracts;
using DiplomaManagementSystem.Application.Authorization;
using DiplomaManagementSystem.Application.Authorization.Contracts;
using DiplomaManagementSystem.Application.Common.Contracts;
using DiplomaManagementSystem.Application.Employee;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Secretary;

internal sealed class SecretaryDiplomaActionService(
    IApplicationDbContext dbContext,
    IArchiveGuard archiveGuard,
    IAuditLogWriter auditLogWriter,
    IDiplomaAuthorizationService diplomaAuthorizationService,
    IDiplomaQueries diplomaQueries,
    IUserDisplayQueries userDisplayQueries,
    ITopicVersionQueries topicVersionQueries,
    IAdmissionStepQueries admissionStepQueries,
    IEmployeeWorkloadLimitService employeeWorkloadLimitService,
    ReviewerAssignmentService reviewerAssignmentService,
    DiplomaAdmissionService diplomaAdmissionService,
    SecretarySupervisorOverrideService supervisorOverrideService,
    AdmissionWorkflowService admissionWorkflowService,
    DiplomaLifecycleService diplomaLifecycleService) : ISecretaryDiplomaActionService
{
    public async Task AssignReviewerAsync(
        Guid actorId,
        Guid sessionId,
        AssignReviewerDto request,
        CancellationToken cancellationToken = default)
    {
        Diploma diploma = await GetWritableDiplomaAsync(sessionId, request.DiplomaId, cancellationToken);
        await diplomaAuthorizationService.EnsureCanPerformAsync(
            actorId,
            request.DiplomaId,
            DiplomaAction.AssignReviewer,
            sessionId,
            cancellationToken);

        bool employeeExists = await userDisplayQueries.IsActiveDepartmentEmployeeAsync(
            request.ReviewerId,
            diploma.DefenceSession.DepartmentId,
            cancellationToken);

        if (!employeeExists)
        {
            throw new DomainException(AuthorizationMessages.ReviewerNotFound);
        }

        Guid? oldReviewerId = diploma.ReviewerId;

        bool hasApprovedTopic = await diplomaQueries.HasApprovedTopicAsync(diploma.Id, cancellationToken);

        if (diploma.ReviewerId != request.ReviewerId)
        {
            await employeeWorkloadLimitService.EnsureCanAssignReviewerAsync(
                sessionId,
                request.ReviewerId,
                diploma.Id,
                cancellationToken);
        }

        reviewerAssignmentService.Assign(
            diploma,
            diploma.DefenceSession,
            request.ReviewerId,
            diploma.AdmissionStepAttempts,
            hasApprovedTopic);

        AuditLogEntry auditEntry = new(
            actorId,
            nameof(Diploma),
            diploma.Id,
            "AssignReviewer",
            oldReviewerId?.ToString(),
            request.ReviewerId.ToString(),
            sessionId);

        await auditLogWriter.WriteAsync(auditEntry, cancellationToken);

        await DiplomaLifecycleHelper.RecalculateAsync(admissionStepQueries, topicVersionQueries, diplomaLifecycleService, diploma, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AdmitAsync(
        Guid actorId,
        Guid sessionId,
        AdmitDiplomaDto request,
        CancellationToken cancellationToken = default)
    {
        Diploma diploma = await GetWritableDiplomaAsync(sessionId, request.DiplomaId, cancellationToken);
        await diplomaAuthorizationService.EnsureCanPerformAsync(
            actorId,
            request.DiplomaId,
            DiplomaAction.AdmitDiploma,
            sessionId,
            cancellationToken);

        DiplomaAdmissionStatus oldStatus = diploma.AdmissionStatus;
        DateOnly? oldDate = diploma.DefenceDate;

        diplomaAdmissionService.Admit(
            diploma,
            diploma.DefenceSession,
            request.DefenceDate,
            diploma.LifecycleStatus);

        AuditLogEntry auditEntry = new(
            actorId,
            nameof(Diploma),
            diploma.Id,
            "Admit",
            $"{oldStatus};{oldDate}",
            $"{diploma.AdmissionStatus};{diploma.DefenceDate}",
            sessionId);

        await auditLogWriter.WriteAsync(auditEntry, cancellationToken);

        await DiplomaLifecycleHelper.RecalculateAsync(admissionStepQueries, topicVersionQueries, diplomaLifecycleService, diploma, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task OverrideSupervisorAsync(
        Guid actorId,
        Guid sessionId,
        OverrideSupervisorDto request,
        CancellationToken cancellationToken = default)
    {
        Diploma diploma = await GetWritableDiplomaAsync(sessionId, request.DiplomaId, cancellationToken);
        await diplomaAuthorizationService.EnsureCanPerformAsync(
            actorId,
            request.DiplomaId,
            DiplomaAction.OverrideSupervisor,
            sessionId,
            cancellationToken);

        bool employeeExists = await userDisplayQueries.IsActiveDepartmentEmployeeAsync(
            request.SupervisorId,
            diploma.DefenceSession.DepartmentId,
            cancellationToken);

        if (!employeeExists)
        {
            throw new DomainException(AuthorizationMessages.SupervisorNotFound);
        }

        Guid? oldSupervisorId = diploma.SupervisorId;

        bool supervisorChanges = diploma.SupervisorId != request.SupervisorId
                                 || diploma.SupervisorAssignmentStatus != SupervisorAssignmentStatus.Confirmed;

        if (supervisorChanges)
        {
            await employeeWorkloadLimitService.EnsureCanAssignSupervisorAsync(
                sessionId,
                request.SupervisorId,
                diploma.Id,
                cancellationToken);
        }

        supervisorOverrideService.Override(diploma, diploma.DefenceSession, request.SupervisorId);

        dbContext.DiplomaComments.Add(new DiplomaComment
        {
            Id = Guid.NewGuid(),
            DiplomaId = diploma.Id,
            AuthorId = actorId,
            Body = $"{WorkflowUkrainianLabels.SupervisorChangeCommentPrefix} {request.Reason.Trim()}",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        AuditLogEntry auditEntry = new(
            actorId,
            nameof(Diploma),
            diploma.Id,
            "OverrideSupervisor",
            oldSupervisorId?.ToString(),
            request.SupervisorId.ToString(),
            sessionId);

        await auditLogWriter.WriteAsync(auditEntry, cancellationToken);

        await DiplomaLifecycleHelper.RecalculateAsync(admissionStepQueries, topicVersionQueries, diplomaLifecycleService, diploma, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AddCommentAsync(
        Guid actorId,
        Guid sessionId,
        AddCommentDto request,
        CancellationToken cancellationToken = default)
    {
        Diploma diploma = await GetWritableDiplomaAsync(sessionId, request.DiplomaId, cancellationToken);
        await diplomaAuthorizationService.EnsureCanPerformAsync(
            actorId,
            request.DiplomaId,
            DiplomaAction.AddSecretaryComment,
            sessionId,
            cancellationToken);

        string body = request.Body.Trim();
        DiplomaComment comment = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diploma.Id,
            AuthorId = actorId,
            Body = body,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.DiplomaComments.Add(comment);

        AuditLogEntry auditEntry = new(
            actorId,
            nameof(Diploma),
            diploma.Id,
            "AddComment",
            null,
            body,
            sessionId);

        await auditLogWriter.WriteAsync(auditEntry, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task OverrideAdmissionStepAsync(
        Guid actorId,
        Guid sessionId,
        OverrideAdmissionStepDto request,
        CancellationToken cancellationToken = default)
    {
        Diploma diploma = await GetWritableDiplomaAsync(sessionId, request.DiplomaId, cancellationToken);
        await diplomaAuthorizationService.EnsureCanPerformAsync(
            actorId,
            request.DiplomaId,
            DiplomaAction.OverrideAdmissionStep,
            sessionId,
            cancellationToken);

        if (!AdmissionStepSequence.AcceptsOutcome(request.Step))
        {
            throw new DomainException(AuthorizationMessages.AdmissionStepOverrideNotAllowed);
        }

        if (diploma.CurrentAdmissionStep != request.Step)
        {
            throw new DomainException(AuthorizationMessages.AdmissionStepOverrideWrongStep);
        }

        string oldValue = $"{diploma.CurrentAdmissionStep};attempts={diploma.AdmissionStepAttempts.Count}";

        DiplomaAdmissionStepAttempt attempt = admissionWorkflowService.RecordAttempt(
            diploma,
            request.Step,
            diploma.AdmissionStepAttempts,
            actorId,
            request.Outcome,
            request.Comment.Trim(),
            isSecretaryOverride: true);

        dbContext.DiplomaAdmissionStepAttempts.Add(attempt);

        AuditLogEntry auditEntry = new(
            actorId,
            nameof(DiplomaAdmissionStepAttempt),
            attempt.Id,
            "SecretaryOverrideAdmissionStep",
            oldValue,
            WorkflowUkrainianLabels.BuildAdmissionStepOverrideAuditNewValue(
                request.Step,
                request.Outcome,
                request.Comment),
            sessionId);

        await auditLogWriter.WriteAsync(auditEntry, cancellationToken);

        dbContext.DiplomaComments.Add(new DiplomaComment
        {
            Id = Guid.NewGuid(),
            DiplomaId = diploma.Id,
            AuthorId = actorId,
            Body = $"{WorkflowUkrainianLabels.BuildAdmissionStepOverrideCommentPrefix(request.Step)} {request.Comment.Trim()}",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await DiplomaLifecycleHelper.RecalculateAsync(admissionStepQueries, topicVersionQueries, diplomaLifecycleService, diploma, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<Diploma> GetWritableDiplomaAsync(
        Guid sessionId,
        Guid diplomaId,
        CancellationToken cancellationToken)
    {
        Diploma? diploma = await diplomaQueries.FindWritableAsync(
            new DiplomaWritableCriteria(diplomaId, SessionId: sessionId, IncludeAdmissionAttempts: true),
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
