using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;

public sealed record DefenceSessionListItemDto(
    Guid Id,
    int Year,
    DefenceSessionType Type,
    int? Semester,
    DefenceSessionStatus Status,
    int GroupCount,
    int DiplomaCount);

public sealed record DefenceSessionFormDto(
    Guid? Id,
    int Year,
    DefenceSessionType Type,
    int? Semester);

public sealed record DefenceSessionDetailsDto(
    Guid Id,
    int Year,
    DefenceSessionType Type,
    int? Semester,
    DefenceSessionStatus Status,
    IReadOnlyList<StudyGroupItemDto> Groups,
    int DiplomaCount);

public sealed record StudyGroupItemDto(
    Guid Id,
    string Name,
    int? Course,
    int StudentCount);
