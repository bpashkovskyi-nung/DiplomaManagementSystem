using DiplomaManagementSystem.Application.Common.Contracts;
using DiplomaManagementSystem.Application.Documents.Contracts;
using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.ReadModels;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Application.Student;

internal sealed class StudentDiplomaService(
    IApplicationDbContext dbContext,
    IDiplomaQueries diplomaQueries,
    IUserDisplayQueries userDisplayQueries,
    ITopicVersionQueries topicVersionQueries,
    IAdmissionStepQueries admissionStepQueries,
    IDiplomaCommentQueries diplomaCommentQueries,
    IDefenceSessionQueries defenceSessionQueries,
    IArchiveGuard archiveGuard,
    SupervisorSelectionService supervisorSelectionService,
    DiplomaTopicService diplomaTopicService,
    DiplomaLifecycleService diplomaLifecycleService,
    WorkReadinessService workReadinessService,
    AdmissionWorkflowService admissionWorkflowService,
    IDiplomaDocumentService diplomaDocumentService) : IStudentDiplomaService
{
    public async Task<MyDiplomaDto> GetMyDiplomaAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        Diploma? diploma = await diplomaQueries.FindLatestForStudentReadAsync(studentId, cancellationToken);

        if (diploma is null)
        {
            DefenceSessionSummary? sessionSummary = await defenceSessionQueries.FindForStudentAsync(
                studentId,
                cancellationToken);

            return BuildEmptyMyDiploma(sessionSummary);
        }

        HashSet<Guid> userIds = [diploma.StudentId];
        if (diploma.SupervisorId.HasValue)
        {
            userIds.Add(diploma.SupervisorId.Value);
        }

        foreach (DiplomaAdmissionStepAttempt attempt in diploma.AdmissionStepAttempts)
        {
            userIds.Add(attempt.RecordedById);
        }

        foreach (DiplomaTopicVersion topicVersion in diploma.TopicVersions)
        {
            if (topicVersion.ReviewedById.HasValue)
            {
                userIds.Add(topicVersion.ReviewedById.Value);
            }

            if (topicVersion.SupervisorReviewedById.HasValue)
            {
                userIds.Add(topicVersion.SupervisorReviewedById.Value);
            }
        }

        Dictionary<Guid, ApplicationUser> users = await userDisplayQueries.LoadUsersAsync(userIds, cancellationToken);

        string? supervisorName = null;
        if (diploma.SupervisorId.HasValue
            && users.TryGetValue(diploma.SupervisorId.Value, out ApplicationUser? supervisor))
        {
            supervisorName = supervisor.FullName;
        }

        List<PersonOptionDto> supervisorOptions = PersonOptionMapping.From(
            await userDisplayQueries.LoadEmployeeOptionsForDepartmentAsync(
                diploma.DefenceSession.DepartmentId,
                cancellationToken));

        DiplomaTopicVersion? latestTopic = diploma.TopicVersions
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();

        List<StudentTopicVersionDto> topicVersions = diploma.TopicVersions
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => new StudentTopicVersionDto(
                version.Id,
                version.VersionNumber,
                version.Title,
                version.Status,
                version.RejectionReason,
                version.SubmittedAt,
                version.ReviewedAt,
                version.ReviewedById.HasValue
                && users.TryGetValue(version.ReviewedById.Value, out ApplicationUser? reviewerUser)
                    ? reviewerUser.FullName
                    : null,
                version.SupervisorReviewedAt,
                version.SupervisorReviewedById.HasValue
                && users.TryGetValue(version.SupervisorReviewedById.Value, out ApplicationUser? supervisorReviewerUser)
                    ? supervisorReviewerUser.FullName
                    : null))
            .ToList();

        List<DiplomaComment> comments = await diplomaCommentQueries.ListForDiplomaReadAsync(
            diploma.Id,
            cancellationToken);

        HashSet<Guid> authorIds = comments.Select(comment => comment.AuthorId).ToHashSet();
        Dictionary<Guid, string> authorNames = await userDisplayQueries.LoadFullNamesAsync(authorIds, cancellationToken);

        List<DiplomaCommentDto> commentDtos = comments
            .Select(comment => new DiplomaCommentDto(
                authorNames.GetValueOrDefault(comment.AuthorId, "—"),
                comment.Body,
                comment.CreatedAt))
            .ToList();

        List<StudentAdmissionStepDto> checkpoints = [];

        bool hasStudentWork = await diplomaDocumentService.HasStudentWorkAsync(diploma.Id, cancellationToken);

        DiplomaWorkflowState workflow = DiplomaWorkflowState.From(
            diploma,
            WorkflowAudience.Student,
            new DiplomaWorkflowOptions(
                supervisorOptions.Count > 0,
                hasStudentWork,
                diplomaTopicService.CanSubmitNewVersion(diploma.TopicVersions)));
        DiplomaWorkflowStudentFlags flags = workflow.Student!;

        DiplomaDocumentsBundleDto documents = await diplomaDocumentService.GetDocumentsAsync(
            diploma.Id,
            cancellationToken);

        DiplomaTopicVersion? approvedTopic = diploma.TopicVersions
            .Where(version => version.Status == TopicVersionStatus.Approved)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();

        string? headApproverName = null;
        if (approvedTopic?.ReviewedById is Guid headApproverId
            && users.TryGetValue(headApproverId, out ApplicationUser? headApprover))
        {
            headApproverName = headApprover.FullName;
        }

        string? supervisorApproverName = null;
        if (approvedTopic?.SupervisorReviewedById is Guid supervisorApproverId
            && users.TryGetValue(supervisorApproverId, out ApplicationUser? supervisorApprover))
        {
            supervisorApproverName = supervisorApprover.FullName;
        }

        TopicApprovalDisplay? topicApproval = approvedTopic is not null
            ? TopicVersionApprovalFormatter.BuildApprovedDisplay(
                approvedTopic,
                supervisorApproverName ?? supervisorName,
                headApproverName)
            : null;

        StudentWorkflowProgressDto workflowProgress = StudentWorkflowProgressBuilder.Build(
            diploma,
            workflow.SessionActive,
            completedByNames: users.ToDictionary(user => user.Key, user => user.Value.FullName),
            people: new WorkflowPersonLabels(supervisorName, null, topicApproval));

        MyDiplomaHeaderDto header = new(
            diploma.Id,
            true,
            SecretarySessionLabel.Format(
                diploma.DefenceSession.Year,
                diploma.DefenceSession.Type,
                diploma.DefenceSession.Semester),
            diploma.DefenceSession.Type);

        MyDiplomaAssignmentsDto assignments = new(
            supervisorName,
            diploma.SupervisorId,
            diploma.SupervisorAssignmentStatus,
            latestTopic?.Title,
            latestTopic?.Status);

        DiplomaLifecycleSnapshotDto state = new(
            diploma.LifecycleStatus,
            diploma.AdmissionStatus,
            diploma.CurrentAdmissionStep,
            diploma.DefenceDate);

        MyDiplomaHistoryDto history = new(checkpoints, topicVersions, commentDtos);

        return new MyDiplomaDto(
            header,
            assignments,
            state,
            history,
            flags,
            workflowProgress,
            documents,
            supervisorOptions);
    }

    public async Task SelectSupervisorAsync(
        Guid studentId,
        SelectSupervisorDto request,
        CancellationToken cancellationToken = default)
    {
        Diploma diploma = await GetWritableDiplomaAsync(studentId, request.DiplomaId, cancellationToken);

        bool employeeExists = await userDisplayQueries.IsActiveDepartmentEmployeeAsync(
            request.SupervisorId,
            diploma.DefenceSession.DepartmentId,
            cancellationToken);

        if (!employeeExists)
        {
            throw new DomainException("Selected supervisor is not a valid employee.");
        }

        supervisorSelectionService.RequestSupervisor(diploma, diploma.DefenceSession, request.SupervisorId);

        DiplomaTopicVersion? latestTopic = await topicVersionQueries.GetLatestAsync(diploma.Id, cancellationToken);
        List<DiplomaAdmissionStepAttempt> attempts = await admissionStepQueries.ListForDiplomaAsync(
            diploma.Id,
            cancellationToken);

        diploma.LifecycleStatus = diplomaLifecycleService.Recalculate(diploma, latestTopic, attempts);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitTopicAsync(
        Guid studentId,
        SubmitTopicDto request,
        CancellationToken cancellationToken = default)
    {
        Diploma diploma = await GetWritableDiplomaAsync(studentId, request.DiplomaId, cancellationToken);

        if (diploma.SupervisorAssignmentStatus != SupervisorAssignmentStatus.Confirmed)
        {
            throw new DomainException("Supervisor must be confirmed before submitting a topic.");
        }

        List<DiplomaTopicVersion> versions = await topicVersionQueries.ListForDiplomaWritableAsync(
            diploma.Id,
            cancellationToken);

        diplomaTopicService.EnsureCanSubmitNewVersion(versions);

        int nextVersion = diplomaTopicService.GetNextVersionNumber(versions);
        DiplomaTopicVersion newVersion = diplomaTopicService.CreateVersion(diploma.Id, request.Title, nextVersion);
        dbContext.DiplomaTopicVersions.Add(newVersion);
        versions.Add(newVersion);

        List<DiplomaAdmissionStepAttempt> attempts = await admissionStepQueries.ListForDiplomaAsync(
            diploma.Id,
            cancellationToken);

        diploma.LifecycleStatus = diplomaLifecycleService.Recalculate(diploma, newVersion, attempts);
        diploma.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeclareWorkReadyAsync(
        Guid studentId,
        Guid diplomaId,
        CancellationToken cancellationToken = default)
    {
        Diploma diploma = await GetWritableDiplomaAsync(studentId, diplomaId, cancellationToken);

        if (diploma.LifecycleStatus == DiplomaLifecycleStatus.DocumentsInProgress
            && diploma.CurrentAdmissionStep == AdmissionStep.SupervisorFeedback)
        {
            return;
        }

        DiplomaTopicVersion? latestTopic = await topicVersionQueries.GetLatestAsync(diploma.Id, cancellationToken);
        if (latestTopic is null)
        {
            throw new DomainException("Approved topic not found.");
        }

        if (!await diplomaDocumentService.HasStudentWorkAsync(diploma.Id, cancellationToken))
        {
            throw new DomainException("Upload at least one work file before declaring readiness.");
        }

        List<DiplomaAdmissionStepAttempt> attempts = await admissionStepQueries.ListForDiplomaAsync(
            diploma.Id,
            cancellationToken);

        workReadinessService.DeclareReady(
            diploma,
            diploma.DefenceSession,
            latestTopic,
            attempts);

        admissionWorkflowService.StartAdmissionReview(
            diploma,
            diploma.DefenceSession,
            latestTopic,
            attempts);

        diploma.LifecycleStatus = diplomaLifecycleService.Recalculate(diploma, latestTopic, attempts);
        diploma.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task UploadWorkAsync(
        Guid studentId,
        Guid diplomaId,
        UploadFileContent file,
        CancellationToken cancellationToken = default)
    {
        return diplomaDocumentService.UploadStudentWorkAsync(studentId, diplomaId, file, cancellationToken);
    }

    private async Task<Diploma> GetWritableDiplomaAsync(
        Guid studentId,
        Guid diplomaId,
        CancellationToken cancellationToken)
    {
        Diploma? diploma = await diplomaQueries.FindWritableAsync(
            new DiplomaWritableCriteria(diplomaId, StudentId: studentId),
            cancellationToken);

        if (diploma is null)
        {
            throw new DomainException("Diploma not found.");
        }

        archiveGuard.EnsureWritable(diploma.DefenceSession);
        return diploma;
    }

    private static MyDiplomaDto BuildEmptyMyDiploma(DefenceSessionSummary? sessionSummary)
    {
        string? sessionLabel = sessionSummary is not null
            ? SecretarySessionLabel.Format(
                sessionSummary.Year,
                sessionSummary.Type,
                sessionSummary.Semester)
            : null;

        MyDiplomaHeaderDto header = new(null, false, sessionLabel, sessionSummary?.Type);
        MyDiplomaAssignmentsDto assignments = new(null, null, null, null, null);
        MyDiplomaHistoryDto history = new([], [], []);

        return new MyDiplomaDto(header, assignments, null, history, null, null, null, []);
    }
}
