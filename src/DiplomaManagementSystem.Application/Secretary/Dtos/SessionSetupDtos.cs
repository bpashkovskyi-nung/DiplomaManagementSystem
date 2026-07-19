namespace DiplomaManagementSystem.Application.Secretary.Dtos;

public sealed record SessionSetupPageDto(
    Guid SessionId,
    string SessionLabel,
    IReadOnlyList<MilestoneSetupItemDto> Milestones,
    IReadOnlyList<DefenceDateOptionDto> AvailableDates);

public sealed record MilestoneSetupItemDto(
    Guid? Id,
    int Ordinal,
    DateOnly? DueDate,
    int? ExpectedPercent);

public sealed record DefenceDateOptionDto(Guid Id, DateOnly Date, bool IsProtected);

public sealed record SaveMilestonesDto(IReadOnlyList<SaveMilestoneItemDto> Milestones);

public sealed record SaveMilestoneItemDto(DateOnly DueDate, int ExpectedPercent);

public sealed record SaveDefenceDatesDto(IReadOnlyList<DateOnly> Dates);

public sealed record DefenceDatePreferenceQueueDto(
    Guid SessionId,
    string SessionLabel,
    IReadOnlyList<DefenceDatePreferenceItemDto> Items,
    IReadOnlyList<DateOnly> AvailableDates);

public sealed record DefenceDatePreferenceItemDto(
    Guid DiplomaId,
    string StudentFullName,
    string StudyGroupName,
    DateOnly PreferredDate,
    DateTimeOffset RequestedAt,
    string RequesterTypeLabel,
    string RequesterName,
    DateOnly? ConfirmedDefenceDate);
