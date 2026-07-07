namespace DiplomaManagementSystem.Application.Secretary.Documents.Dtos;

public sealed record TopicOrderFormDto(
    Guid SessionId,
    string SessionLabel,
    IReadOnlyList<TopicOrderStudyGroupOptionDto> StudyGroups,
    int DefaultYear);

public sealed record TopicOrderStudyGroupOptionDto(
    Guid Id,
    string Name,
    int? Course);

public sealed record TopicOrderGenerateRequestDto(
    Guid SessionId,
    string OrderNumber,
    int Year,
    IReadOnlyList<Guid> StudyGroupIds);

public sealed record TopicOrderStudentRowDto(
    string StudentFullName,
    string TopicTitle,
    string SupervisorLine);

public sealed record TopicOrderReviewerRowDto(
    string ReviewerLine,
    int AssignmentCount);

public sealed record TopicOrderDocumentDto(
    string OrderNumber,
    int Year,
    string SessionLevelPhrase,
    string GroupsPhrase,
    string CoursePhrase,
    IReadOnlyList<TopicOrderStudentRowDto> Students,
    IReadOnlyList<TopicOrderReviewerRowDto> Reviewers,
    string? FormattingReviewerLine,
    string? DepartmentHeadLine,
    IReadOnlyList<string> Warnings);

public sealed record TopicOrderPreviewDto(
    TopicOrderDocumentDto Document,
    bool CanGenerate);
