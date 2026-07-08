namespace DiplomaManagementSystem.Application.SuperAdmin.Departments.Dtos;

public sealed record DepartmentListItemDto(
    Guid Id,
    Guid FacultyId,
    string FacultyName,
    string Name,
    int SpecialtyCount,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record DepartmentFormDto(
    Guid? Id,
    Guid FacultyId,
    string Name);
