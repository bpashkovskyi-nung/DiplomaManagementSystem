using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;

namespace DiplomaManagementSystem.Application.Employee;

internal static class EmployeeDiplomaListProjection
{
    private const string MissingLabel = "—";

    public static async Task<IReadOnlyList<PendingStudentDto>> MapPendingStudentsAsync(
        IUserDisplayQueries userDisplayQueries,
        IReadOnlyList<Domain.Entities.Diploma> diplomas,
        CancellationToken cancellationToken)
    {
        if (diplomas.Count == 0)
        {
            return [];
        }

        HashSet<Guid> studentIds = diplomas.Select(diploma => diploma.StudentId).ToHashSet();
        Dictionary<Guid, StudentDisplayInfo> displays = await userDisplayQueries.LoadStudentDisplaysAsync(
            studentIds,
            cancellationToken);

        return diplomas
            .Select(diploma =>
            {
                displays.TryGetValue(diploma.StudentId, out StudentDisplayInfo? display);
                return new PendingStudentDto(
                    diploma.Id,
                    display?.FullName ?? MissingLabel,
                    display?.GroupName ?? MissingLabel,
                    diploma.UpdatedAt);
            })
            .OrderBy(item => PersonNameSort.SurnameKey(item.StudentFullName), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.StudentFullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static Task<IReadOnlyList<PendingCheckpointItemDto>> MapPendingCheckpointItemsAsync(
        IUserDisplayQueries userDisplayQueries,
        ITopicVersionQueries topicVersionQueries,
        IReadOnlyList<Domain.Entities.Diploma> diplomas,
        IReadOnlyDictionary<Guid, DiplomaDocumentDto> latestStudentWorkByDiplomaId,
        CancellationToken cancellationToken)
    {
        if (diplomas.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<PendingCheckpointItemDto>>([]);
        }

        return MapPendingCheckpointItemsCoreAsync(
            userDisplayQueries,
            topicVersionQueries,
            diplomas,
            latestStudentWorkByDiplomaId,
            cancellationToken);
    }

    private static async Task<IReadOnlyList<PendingCheckpointItemDto>> MapPendingCheckpointItemsCoreAsync(
        IUserDisplayQueries userDisplayQueries,
        ITopicVersionQueries topicVersionQueries,
        IReadOnlyList<Domain.Entities.Diploma> diplomas,
        IReadOnlyDictionary<Guid, DiplomaDocumentDto> latestStudentWorkByDiplomaId,
        CancellationToken cancellationToken)
    {
        HashSet<Guid> studentIds = diplomas.Select(diploma => diploma.StudentId).ToHashSet();
        Dictionary<Guid, StudentDisplayInfo> displays = await userDisplayQueries.LoadStudentDisplaysAsync(
            studentIds,
            cancellationToken);

        HashSet<Guid> diplomaIds = diplomas.Select(diploma => diploma.Id).ToHashSet();
        Dictionary<Guid, string> topicTitles = await topicVersionQueries.GetApprovedTitlesAsync(
            diplomaIds,
            cancellationToken);

        return diplomas
            .Select(diploma =>
            {
                displays.TryGetValue(diploma.StudentId, out StudentDisplayInfo? display);
                topicTitles.TryGetValue(diploma.Id, out string? topicTitle);
                latestStudentWorkByDiplomaId.TryGetValue(diploma.Id, out DiplomaDocumentDto? latestWork);

                return new PendingCheckpointItemDto(
                    diploma.Id,
                    display?.FullName ?? MissingLabel,
                    display?.GroupName ?? MissingLabel,
                    topicTitle,
                    MapStudentWorkLink(latestWork));
            })
            .OrderBy(item => PersonNameSort.SurnameKey(item.StudentFullName), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.StudentFullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static async Task<IReadOnlyList<ReviewerAssignmentItemDto>> MapReviewerAssignmentsAsync(
        IUserDisplayQueries userDisplayQueries,
        ITopicVersionQueries topicVersionQueries,
        IReadOnlyList<Domain.Entities.Diploma> diplomas,
        IReadOnlyDictionary<Guid, DiplomaDocumentDto> latestStudentWorkByDiplomaId,
        CancellationToken cancellationToken)
    {
        if (diplomas.Count == 0)
        {
            return [];
        }

        HashSet<Guid> studentIds = diplomas.Select(diploma => diploma.StudentId).ToHashSet();
        Dictionary<Guid, StudentDisplayInfo> displays = await userDisplayQueries.LoadStudentDisplaysAsync(
            studentIds,
            cancellationToken);

        HashSet<Guid> diplomaIds = diplomas.Select(diploma => diploma.Id).ToHashSet();
        Dictionary<Guid, string> topicTitles = await topicVersionQueries.GetApprovedTitlesAsync(
            diplomaIds,
            cancellationToken);

        return diplomas
            .Select(diploma =>
            {
                displays.TryGetValue(diploma.StudentId, out StudentDisplayInfo? display);
                latestStudentWorkByDiplomaId.TryGetValue(diploma.Id, out DiplomaDocumentDto? latestWork);

                return new ReviewerAssignmentItemDto(
                    diploma.Id,
                    display?.FullName ?? MissingLabel,
                    display?.GroupName ?? MissingLabel,
                    topicTitles.GetValueOrDefault(diploma.Id, MissingLabel),
                    diploma.ReviewAssignmentStatus,
                    MapStudentWorkLink(latestWork));
            })
            .OrderBy(item => PersonNameSort.SurnameKey(item.StudentFullName), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.StudentFullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static PendingStudentWorkLinkDto? MapStudentWorkLink(DiplomaDocumentDto? document) =>
        document is null
            ? null
            : new PendingStudentWorkLinkDto(document.FileName, document.ViewUrl, document.VersionNumber);

    public static async Task<IReadOnlyList<TopicReviewItemDto>> MapTopicReviewItemsAsync(
        IUserDisplayQueries userDisplayQueries,
        IReadOnlyList<Domain.Entities.DiplomaTopicVersion> versions,
        CancellationToken cancellationToken)
    {
        if (versions.Count == 0)
        {
            return [];
        }

        HashSet<Guid> studentIds = versions.Select(version => version.Diploma.StudentId).ToHashSet();
        Dictionary<Guid, string> studentNames = await userDisplayQueries.LoadFullNamesAsync(
            studentIds,
            cancellationToken);

        HashSet<Guid> supervisorIds = versions
            .Select(version => version.Diploma.SupervisorId)
            .Where(supervisorId => supervisorId.HasValue)
            .Select(supervisorId => supervisorId!.Value)
            .ToHashSet();
        Dictionary<Guid, string> supervisorNames = supervisorIds.Count == 0
            ? []
            : await userDisplayQueries.LoadFullNamesAsync(supervisorIds, cancellationToken);

        return versions
            .Select(version =>
            {
                string? supervisorFullName = version.Diploma.SupervisorId is Guid supervisorId
                    ? supervisorNames.GetValueOrDefault(supervisorId, MissingLabel)
                    : null;

                return new TopicReviewItemDto(
                    version.Id,
                    version.DiplomaId,
                    studentNames.GetValueOrDefault(version.Diploma.StudentId, MissingLabel),
                    supervisorFullName,
                    version.Title,
                    version.VersionNumber,
                    version.SubmittedAt);
            })
            .OrderBy(item => PersonNameSort.SurnameKey(item.StudentFullName), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.StudentFullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }
}
