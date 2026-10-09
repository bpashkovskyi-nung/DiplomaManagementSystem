using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Secretary.Dtos;

public sealed record SecretarySessionOptionDto(Guid Id, string Label);

public sealed record SecretaryDashboardBucketDto(
    DiplomaLifecycleStatus? LifecycleStatus,
    AdmissionStep? AdmissionStep,
    int Count);

public sealed record SecretaryDashboardDto(
    Guid SessionId,
    DefenceSessionType SessionType,
    string SessionLabel,
    IReadOnlyList<SecretaryDashboardBucketDto> Buckets,
    int TotalDiplomas);

public sealed record DiplomaListFilterDto(
    DiplomaLifecycleStatus? LifecycleStatus,
    AdmissionStep? CurrentAdmissionStep,
    SupervisorAssignmentStatus? SupervisorAssignmentStatus,
    DiplomaAdmissionStatus? AdmissionStatus,
    Guid? StudyGroupId,
    string? Search);

public sealed record DiplomaListItemDto(
    Guid Id,
    string StudentFullName,
    string StudentEmail,
    string StudyGroupName,
    string? SupervisorName,
    string? TopicTitle,
    DiplomaLifecycleStatus LifecycleStatus,
    DiplomaAdmissionStatus AdmissionStatus,
    AdmissionStep? CurrentAdmissionStep,
    int OutcomeStepsCompleted,
    int OutcomeStepsTotal);

public sealed record DiplomaListPageDto(
    Guid SessionId,
    DefenceSessionType SessionType,
    IReadOnlyList<DiplomaListItemDto> Items,
    DiplomaListFilterDto Filter,
    IReadOnlyList<StudyGroupFilterOptionDto> StudyGroups);

public sealed record StudyGroupFilterOptionDto(Guid Id, string Name);

public sealed record AdmissionStepStatusDto(
    AdmissionStep Step,
    bool IsPassing,
    CheckpointOutcome? Outcome,
    string? Comment,
    Guid? RecordedById,
    string? RecordedByName,
    DateTimeOffset? RecordedAt,
    bool IsSecretaryOverride,
    int AttemptCount);

public sealed record AdmissionStepAttemptDto(
    AdmissionStep Step,
    int AttemptNumber,
    CheckpointOutcome Outcome,
    string? Comment,
    string RecordedByName,
    DateTimeOffset RecordedAt,
    bool IsSecretaryOverride);

public sealed record SecretaryTopicVersionDto(
    Guid VersionId,
    int VersionNumber,
    string Title,
    TopicVersionStatus Status,
    string? RejectionReason,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? ReviewedAt,
    string? ReviewedByName,
    DateTimeOffset? SupervisorReviewedAt,
    string? SupervisorReviewedByName);

public sealed record AssignReviewerDto(Guid DiplomaId, Guid ReviewerId);

public sealed record AdmitDiplomaDto(Guid DiplomaId);

public sealed record ConfirmDefenceDateDto(Guid DiplomaId, DateOnly DefenceDate);

public sealed record OverrideSupervisorDto(Guid DiplomaId, Guid SupervisorId, string Reason);

public sealed record AddCommentDto(Guid DiplomaId, string Body);

public sealed record OverrideAdmissionStepDto(
    Guid DiplomaId,
    AdmissionStep Step,
    CheckpointOutcome Outcome,
    string Comment);

public sealed record AdmittedReportDto(
    Guid SessionId,
    string SessionLabel,
    IReadOnlyList<AdmittedReportItemDto> Items);

public sealed record AdmittedReportItemDto(
    string StudentFullName,
    string StudyGroupName,
    string TopicTitle,
    string SupervisorName,
    string? ReviewerName,
    DateOnly? DefenceDate);
