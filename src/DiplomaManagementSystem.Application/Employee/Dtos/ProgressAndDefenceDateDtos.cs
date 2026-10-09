namespace DiplomaManagementSystem.Application.Employee.Dtos;

public sealed record SupervisorProgressPageDto(
    Guid SessionId,
    string SessionLabel,
    IReadOnlyList<MilestoneColumnDto> Milestones,
    IReadOnlyList<SupervisorProgressStudentDto> Students);

public sealed record MilestoneColumnDto(
    Guid MilestoneId,
    int Ordinal,
    DateOnly DueDate,
    int ExpectedPercent);

public sealed record SupervisorProgressStudentDto(
    Guid DiplomaId,
    string StudentFullName,
    string StudyGroupName,
    IReadOnlyList<SupervisorProgressCellDto> Cells);

public sealed record SupervisorProgressCellDto(
    Guid MilestoneId,
    int? ActualPercent);

public sealed record SetMilestoneProgressDto(
    Guid DiplomaId,
    Guid MilestoneId,
    int ActualPercent);

public sealed record DepartmentProgressReportDto(
    Guid SessionId,
    string SessionLabel,
    IReadOnlyList<MilestoneColumnDto> Milestones,
    IReadOnlyList<DepartmentProgressSupervisorGroupDto> Groups);

public sealed record DepartmentProgressSupervisorGroupDto(
    string SupervisorName,
    IReadOnlyList<DepartmentProgressStudentDto> Students);

public sealed record DepartmentProgressStudentDto(
    Guid DiplomaId,
    string StudentFullName,
    string StudyGroupName,
    IReadOnlyList<int?> ActualPercents);

public sealed record DefenceDateRequestFormDto(
    Guid DiplomaId,
    string StudentFullName,
    bool CanRequest,
    DateOnly? PreferredDate,
    DateOnly? ConfirmedDefenceDate,
    IReadOnlyList<DefenceDateChoiceDto> AvailableDates);

public sealed record DefenceDateChoiceDto(Guid OptionId, DateOnly Date);

public sealed record RequestDefenceDateDto(Guid DiplomaId, Guid DefenceDateOptionId);
