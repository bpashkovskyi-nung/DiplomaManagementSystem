using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Employee.Dtos;

public sealed record PendingStudentWorkLinkDto(
    string FileName,
    string ViewUrl,
    int VersionNumber);

public sealed record PendingCheckpointItemDto(
    Guid DiplomaId,
    string StudentFullName,
    string StudyGroupName,
    string? TopicTitle,
    PendingStudentWorkLinkDto? LatestStudentWork = null);

public sealed record CompleteCheckpointDto(
    Guid DiplomaId,
    CheckpointOutcome Outcome,
    string? Comment);

public sealed record ReviewerAssignmentItemDto(
    Guid DiplomaId,
    string StudentFullName,
    string StudyGroupName,
    string TopicTitle,
    ReviewAssignmentStatus ReviewAssignmentStatus,
    PendingStudentWorkLinkDto? LatestStudentWork = null);
