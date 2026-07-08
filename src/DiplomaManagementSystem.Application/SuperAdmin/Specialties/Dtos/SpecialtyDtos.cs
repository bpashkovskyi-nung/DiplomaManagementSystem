namespace DiplomaManagementSystem.Application.SuperAdmin.Specialties.Dtos;

public sealed record SpecialtyListItemDto(
    Guid Id,
    string Code,
    string Name,
    bool IsActive,
    int StudyGroupCount,
    DateTimeOffset CreatedAt);

public sealed record SpecialtyFormDto(
    Guid? Id,
    Guid DepartmentId,
    string Code,
    string Name);

public sealed record SpecialtyOptionDto(
    Guid Id,
    string Label);
