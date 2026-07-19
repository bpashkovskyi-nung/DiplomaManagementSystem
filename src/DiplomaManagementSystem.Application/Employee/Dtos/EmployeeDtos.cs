
using DiplomaManagementSystem.Application.Secretary.Dtos;

namespace DiplomaManagementSystem.Application.Employee.Dtos;

public sealed record EmployeeHomeDto(IReadOnlyList<EmployeeRoleCardDto> Roles);

public sealed record EmployeeRoleCardDto(
    string RoleKey,
    string RoleDisplay,
    int PendingCount,
    string Controller,
    string Action,
    bool CountsStudents = false);

public sealed record PendingStudentDto(
    Guid DiplomaId,
    string StudentFullName,
    string StudyGroupName,
    DateTimeOffset RequestedAt);

public sealed record TopicReviewItemDto(
    Guid VersionId,
    Guid DiplomaId,
    string StudentFullName,
    string? SupervisorFullName,
    string Title,
    int VersionNumber,
    DateTimeOffset SubmittedAt);

public sealed record SupervisorActionDto(Guid DiplomaId, string? Comment);

public sealed record SupervisorDiplomaListPageDto(
    IReadOnlyList<DiplomaListItemDto> Items,
    DiplomaListFilterDto Filter,
    IReadOnlyList<StudyGroupFilterOptionDto> StudyGroups);

public sealed record ReviewerDiplomaListPageDto(
    IReadOnlyList<DiplomaListItemDto> Items,
    DiplomaListFilterDto Filter,
    IReadOnlyList<StudyGroupFilterOptionDto> StudyGroups);

public sealed record ApproveTopicDto(Guid VersionId, string? Comment);

public sealed record ReviewTopicDto(Guid VersionId, string? RejectionReason);
