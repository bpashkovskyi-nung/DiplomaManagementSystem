namespace DiplomaManagementSystem.Application.SuperAdmin.Departments.Dtos;

public sealed record DepartmentListItemDto(
    Guid Id,
    Guid FacultyId,
    string FacultyName,
    string Name,
    string SpecialtyCode,
    string SpecialtyName,
    string StudyForm,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record DepartmentFormDto(
    Guid? Id,
    Guid FacultyId,
    string Name,
    string SpecialtyCode,
    string SpecialtyName,
    string StudyForm);
