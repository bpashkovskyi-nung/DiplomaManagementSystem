using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Admin.Employees.Dtos;

public sealed record EmployeeListItemDto(
    Guid Id,
    string FullName,
    string Email,
    EmployeeAcademicRank? AcademicRank,
    DateTimeOffset CreatedAt);

public sealed record EmployeeFormDto(
    Guid? Id,
    string FullName,
    string Email,
    EmployeeAcademicRank? AcademicRank = null,
    string? ShortDisplayName = null);

public sealed record EmployeeDetailsDto(
    Guid Id,
    string FullName,
    string Email,
    EmployeeAcademicRank? AcademicRank,
    bool HasAssignments,
    DateTimeOffset CreatedAt);
