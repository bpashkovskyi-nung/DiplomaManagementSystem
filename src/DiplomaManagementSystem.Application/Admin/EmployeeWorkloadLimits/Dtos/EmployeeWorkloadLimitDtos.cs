namespace DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;

public sealed record EmployeeWorkloadLimitRowDto(
    Guid EmployeeId,
    string FullName,
    string Email,
    int? MaxSupervisorStudents,
    int? MaxReviewerStudents,
    int ConfirmedSupervisorCount,
    int ReviewerAssignmentCount);

public sealed record EmployeeWorkloadLimitsPageDto(
    Guid DefenceSessionId,
    string SessionLabel,
    IReadOnlyList<EmployeeWorkloadLimitRowDto> Rows);

public sealed record SetEmployeeWorkloadLimitDto(
    Guid DefenceSessionId,
    Guid EmployeeId,
    int? MaxSupervisorStudents,
    int? MaxReviewerStudents);
