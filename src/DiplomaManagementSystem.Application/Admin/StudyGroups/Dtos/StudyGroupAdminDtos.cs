namespace DiplomaManagementSystem.Application.Admin.StudyGroups.Dtos;

public sealed record StudyGroupListItemDto(
    Guid Id,
    string Name,
    int? Course,
    Guid DefenceSessionId,
    string SessionLabel,
    string SpecialtyLabel,
    string StudyForm,
    int StudentCount);

public sealed record StudyGroupFormDto(
    Guid? Id,
    Guid DefenceSessionId,
    string Name,
    Guid SpecialtyId,
    string StudyForm,
    int? Course = null,
    string? SessionLabel = null);
