using DiplomaManagementSystem.Application.Documents.Contracts;
using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.ReadModels;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Application.Student;
using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Secretary;

internal sealed class DiplomaDetailsAssembler(
    IUserDisplayQueries userDisplayQueries,
    IStudyGroupQueries studyGroupQueries,
    IDiplomaCommentQueries diplomaCommentQueries,
    IDiplomaDocumentService diplomaDocumentService)
{
    public async Task<DiplomaDetailsContext> LoadContextAsync(
        Diploma diploma,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> userIds = CollectReferencedUserIds(diploma);
        Dictionary<Guid, ApplicationUser> users = await userDisplayQueries.LoadUsersAsync(userIds, cancellationToken);

        users.TryGetValue(diploma.StudentId, out ApplicationUser? student);

        string studyGroupName = "?";
        if (student?.StudyGroupId is Guid groupId)
        {
            studyGroupName = await studyGroupQueries.GetNameAsync(groupId, cancellationToken) ?? "?";
        }

        string? supervisorName = null;
        if (diploma.SupervisorId.HasValue
            && users.TryGetValue(diploma.SupervisorId.Value, out ApplicationUser? supervisor))
        {
            supervisorName = supervisor.FullName;
        }

        string? reviewerName = null;
        if (diploma.ReviewerId.HasValue
            && users.TryGetValue(diploma.ReviewerId.Value, out ApplicationUser? reviewer))
        {
            reviewerName = reviewer.FullName;
        }

        List<DiplomaComment> comments = await diplomaCommentQueries.ListForDiplomaReadAsync(
            diploma.Id,
            cancellationToken);

        HashSet<Guid> authorIds = comments.Select(comment => comment.AuthorId).ToHashSet();
        Dictionary<Guid, string> commentAuthorNames = await userDisplayQueries.LoadFullNamesAsync(
            authorIds,
            cancellationToken);

        List<PersonOptionDto> employeePool = await LoadEmployeePoolAsync(diploma.DefenceSession.DepartmentId, cancellationToken);
        DiplomaDocumentsBundleDto documents = await diplomaDocumentService.GetDocumentsAsync(
            diploma.Id,
            cancellationToken);

        bool sessionActive = diploma.DefenceSession.Status == DefenceSessionStatus.Active;

        return new DiplomaDetailsContext(
            diploma,
            users,
            student,
            studyGroupName,
            supervisorName,
            reviewerName,
            comments,
            commentAuthorNames,
            employeePool,
            documents,
            sessionActive);
    }

    public static DiplomaDetailsHistory BuildHistory(DiplomaDetailsContext context)
    {
        List<AdmissionStepStatusDto> admissionSteps = BuildAdmissionStepStatusDtos(
            context.Diploma.AdmissionStepAttempts,
            context.Users);

        List<AdmissionStepAttemptDto> attemptHistory = BuildAttemptHistoryDtos(
            context.Diploma.AdmissionStepAttempts,
            context.Users);

        List<SecretaryTopicVersionDto> topicVersions = context.Diploma.TopicVersions
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => new SecretaryTopicVersionDto(
                version.Id,
                version.VersionNumber,
                version.Title,
                version.Status,
                version.RejectionReason,
                version.SubmittedAt,
                version.ReviewedAt,
                version.ReviewedById.HasValue
                && context.Users.TryGetValue(version.ReviewedById.Value, out ApplicationUser? reviewerUser)
                    ? reviewerUser.FullName
                    : null,
                version.SupervisorReviewedAt,
                version.SupervisorReviewedById.HasValue
                && context.Users.TryGetValue(version.SupervisorReviewedById.Value, out ApplicationUser? supervisorReviewerUser)
                    ? supervisorReviewerUser.FullName
                    : null))
            .ToList();

        List<DiplomaCommentDto> commentDtos = context.Comments
            .Select(comment => new DiplomaCommentDto(
                context.CommentAuthorNames.GetValueOrDefault(comment.AuthorId, "—"),
                comment.Body,
                comment.CreatedAt))
            .ToList();

        return new DiplomaDetailsHistory(admissionSteps, attemptHistory, topicVersions, commentDtos);
    }

    public static DiplomaDetailsScreenParts BuildScreenParts(DiplomaDetailsContext context) =>
        BuildScreenParts(context, readOnlyActions: null);

    public static DiplomaDetailsScreenParts BuildReadOnlyScreenParts(DiplomaDetailsContext context) =>
        BuildScreenParts(context, DiplomaWorkflowSecretaryFlags.ReadOnly);

    private static DiplomaDetailsScreenParts BuildScreenParts(
        DiplomaDetailsContext context,
        DiplomaWorkflowSecretaryFlags? readOnlyActions)
    {
        Diploma diploma = context.Diploma;

        DiplomaWorkflowSecretaryFlags actions = readOnlyActions
                                              ?? DiplomaWorkflowState.From(
                                                  diploma,
                                                  WorkflowAudience.Secretary,
                                                  new DiplomaWorkflowOptions(context.EmployeePool.Count > 0)).Secretary!;

        DiplomaTopicVersion? approvedTopic = diploma.TopicVersions
            .Where(version => version.Status == TopicVersionStatus.Approved)
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();

        string? headApproverName = null;
        if (approvedTopic?.ReviewedById is Guid headApproverId
            && context.Users.TryGetValue(headApproverId, out ApplicationUser? headApprover))
        {
            headApproverName = headApprover.FullName;
        }

        string? supervisorApproverName = null;
        if (approvedTopic?.SupervisorReviewedById is Guid supervisorApproverId
            && context.Users.TryGetValue(supervisorApproverId, out ApplicationUser? supervisorApprover))
        {
            supervisorApproverName = supervisorApprover.FullName;
        }

        TopicApprovalDisplay? topicApproval = approvedTopic is not null
            ? TopicVersionApprovalFormatter.BuildApprovedDisplay(
                approvedTopic,
                supervisorApproverName ?? context.SupervisorName,
                headApproverName)
            : null;

        StudentWorkflowProgressDto workflowProgress = StudentWorkflowProgressBuilder.Build(
            diploma,
            context.SessionActive,
            WorkflowAudience.Secretary,
            completedByNames: context.Users.ToDictionary(user => user.Key, user => user.Value.FullName),
            people: new WorkflowPersonLabels(context.SupervisorName, context.ReviewerName, topicApproval));

        return new DiplomaDetailsScreenParts(actions, workflowProgress);
    }

    public static DiplomaDetailsDto Assemble(
        Guid sessionId,
        DiplomaDetailsContext context,
        DiplomaDetailsHistory history,
        DiplomaDetailsScreenParts screenParts)
    {
        Diploma diploma = context.Diploma;

        DiplomaDetailsHeaderDto header = new(
            diploma.Id,
            sessionId,
            diploma.DefenceSession.Type,
            context.Student?.FullName ?? "—",
            context.Student?.Email ?? string.Empty,
            context.StudyGroupName);

        DiplomaAssignmentsDto assignments = new(
            diploma.SupervisorId,
            context.SupervisorName,
            diploma.SupervisorAssignmentStatus,
            diploma.ReviewerId,
            context.ReviewerName,
            diploma.ReviewAssignmentStatus);

        DiplomaLifecycleSnapshotDto state = new(
            diploma.LifecycleStatus,
            diploma.AdmissionStatus,
            diploma.CurrentAdmissionStep,
            diploma.DefenceDate);

        DiplomaDetailsHistoryDto historyDto = new(
            history.AdmissionSteps,
            history.AttemptHistory,
            history.TopicVersions,
            history.Comments);

        return new DiplomaDetailsDto(
            header,
            assignments,
            state,
            historyDto,
            screenParts.Actions,
            screenParts.WorkflowProgress,
            context.Documents,
            context.EmployeePool);
    }

    private static HashSet<Guid> CollectReferencedUserIds(Diploma diploma)
    {
        HashSet<Guid> userIds = [diploma.StudentId];

        if (diploma.SupervisorId.HasValue)
        {
            userIds.Add(diploma.SupervisorId.Value);
        }

        if (diploma.ReviewerId.HasValue)
        {
            userIds.Add(diploma.ReviewerId.Value);
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

        return userIds;
    }

    private async Task<List<PersonOptionDto>> LoadEmployeePoolAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        List<UserOption> employees = await userDisplayQueries.LoadEmployeeOptionsForDepartmentAsync(
            departmentId,
            cancellationToken);
        return PersonOptionMapping.From(employees);
    }

    private static List<AdmissionStepStatusDto> BuildAdmissionStepStatusDtos(
        IEnumerable<DiplomaAdmissionStepAttempt> attempts,
        Dictionary<Guid, ApplicationUser> users)
    {
        List<DiplomaAdmissionStepAttempt> attemptList = attempts.ToList();

        return AdmissionStepSequence.OutcomeSteps
            .Select(step =>
            {
                List<DiplomaAdmissionStepAttempt> stepAttempts = attemptList
                    .Where(attempt => attempt.Step == step)
                    .ToList();

                DiplomaAdmissionStepAttempt? lastPassing =
                    AdmissionStepStatusResolver.GetLastPassingAttempt(step, stepAttempts);

                DiplomaAdmissionStepAttempt? display = lastPassing
                                                       ?? AdmissionStepStatusResolver.GetLastAttempt(step, stepAttempts);

                string? recordedByName = null;

                if (display is not null && users.TryGetValue(display.RecordedById, out ApplicationUser? user))
                {
                    recordedByName = user.FullName;
                }

                return new AdmissionStepStatusDto(
                    step,
                    lastPassing is not null,
                    display?.Outcome,
                    display?.Comment,
                    display?.RecordedById,
                    recordedByName,
                    display?.RecordedAt,
                    display?.IsSecretaryOverride ?? false,
                    stepAttempts.Count);
            })
            .ToList();
    }

    private static List<AdmissionStepAttemptDto> BuildAttemptHistoryDtos(
        IEnumerable<DiplomaAdmissionStepAttempt> attempts,
        Dictionary<Guid, ApplicationUser> users)
    {
        return attempts
            .OrderBy(attempt => attempt.Step)
            .ThenBy(attempt => attempt.AttemptNumber)
            .Select(attempt => new AdmissionStepAttemptDto(
                attempt.Step,
                attempt.AttemptNumber,
                attempt.Outcome,
                attempt.Comment,
                users.TryGetValue(attempt.RecordedById, out ApplicationUser? user)
                    ? user.FullName
                    : "—",
                attempt.RecordedAt,
                attempt.IsSecretaryOverride))
            .ToList();
    }
}

internal sealed record DiplomaDetailsContext(
    Diploma Diploma,
    Dictionary<Guid, ApplicationUser> Users,
    ApplicationUser? Student,
    string StudyGroupName,
    string? SupervisorName,
    string? ReviewerName,
    IReadOnlyList<DiplomaComment> Comments,
    Dictionary<Guid, string> CommentAuthorNames,
    IReadOnlyList<PersonOptionDto> EmployeePool,
    DiplomaDocumentsBundleDto Documents,
    bool SessionActive);

internal sealed record DiplomaDetailsHistory(
    IReadOnlyList<AdmissionStepStatusDto> AdmissionSteps,
    IReadOnlyList<AdmissionStepAttemptDto> AttemptHistory,
    IReadOnlyList<SecretaryTopicVersionDto> TopicVersions,
    IReadOnlyList<DiplomaCommentDto> Comments);

internal sealed record DiplomaDetailsScreenParts(
    DiplomaWorkflowSecretaryFlags Actions,
    StudentWorkflowProgressDto WorkflowProgress);
