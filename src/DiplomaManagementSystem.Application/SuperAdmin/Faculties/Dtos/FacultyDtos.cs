namespace DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;

public sealed record FacultyListItemDto(
    Guid Id,
    string Name,
    bool IsActive,
    int DepartmentCount,
    DateTimeOffset CreatedAt);

public sealed record FacultyFormDto(
    Guid? Id,
    string Name);
