namespace DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Dtos;

public sealed record DepartmentAdminListItemDto(
    Guid AssignmentId,
    Guid UserId,
    string FullName,
    string Email,
    DateTimeOffset AssignedAt);

public sealed record DepartmentAdminAssignDto(
    Guid DepartmentId,
    string Email);
