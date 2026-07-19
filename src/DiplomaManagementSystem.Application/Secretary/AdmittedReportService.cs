using System.Globalization;
using System.Text;

using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;

namespace DiplomaManagementSystem.Application.Secretary;

internal sealed class AdmittedReportService(
    IDefenceSessionQueries defenceSessionQueries,
    IDiplomaQueries diplomaQueries,
    IUserDisplayQueries userDisplayQueries,
    ITopicVersionQueries topicVersionQueries) : IAdmittedReportService
{
    private const string MissingLabel = "—";

    public async Task<AdmittedReportDto?> GetReportAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        DefenceSessionSummary? session = await defenceSessionQueries.FindSummaryAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }

        List<AdmittedReportItemDto> items = await BuildItemsAsync(sessionId, cancellationToken);
        string sessionLabel = SecretarySessionLabel.Format(
            session.Year,
            session.Type,
            session.Semester);

        return new AdmittedReportDto(sessionId, sessionLabel, items);
    }

    public async Task<byte[]> ExportCsvAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        AdmittedReportDto? report = await GetReportAsync(sessionId, cancellationToken);
        if (report is null)
        {
            return [];
        }

        StringBuilder builder = new();
        builder.AppendLine("Student,StudyGroup,Topic,Supervisor,Reviewer,DefenceDate");
        foreach (AdmittedReportItemDto item in report.Items)
        {
            builder.Append(CsvEscape(item.StudentFullName));
            builder.Append(',');
            builder.Append(CsvEscape(item.StudyGroupName));
            builder.Append(',');
            builder.Append(CsvEscape(item.TopicTitle));
            builder.Append(',');
            builder.Append(CsvEscape(item.SupervisorName));
            builder.Append(',');
            builder.Append(CsvEscape(item.ReviewerName ?? string.Empty));
            builder.Append(',');
            builder.Append(CsvEscape(item.DefenceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
            builder.AppendLine();
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    private async Task<List<AdmittedReportItemDto>> BuildItemsAsync(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        List<Diploma> diplomas = await diplomaQueries.ListAdmittedForSessionAsync(sessionId, cancellationToken);
        if (diplomas.Count == 0)
        {
            return [];
        }

        var userIds = diplomas
            .Select(diploma => diploma.StudentId)
            .Concat(diplomas.Where(diploma => diploma.SupervisorId.HasValue).Select(diploma => diploma.SupervisorId!.Value))
            .Concat(diplomas.Where(diploma => diploma.ReviewerId.HasValue).Select(diploma => diploma.ReviewerId!.Value))
            .ToHashSet();

        Dictionary<Guid, ApplicationUser> users = await userDisplayQueries.LoadUsersAsync(userIds, cancellationToken);

        var groupIds = users.Values
            .Where(user => user.StudyGroupId.HasValue)
            .Select(user => user.StudyGroupId!.Value)
            .ToHashSet();

        Dictionary<Guid, string> groupNames = await userDisplayQueries.LoadStudyGroupNamesAsync(
            groupIds,
            cancellationToken);

        var diplomaIds = diplomas.Select(diploma => diploma.Id).ToHashSet();
        Dictionary<Guid, string> topicTitles = await topicVersionQueries.GetApprovedTitlesAsync(
            diplomaIds,
            cancellationToken);

        return diplomas
            .Select(diploma =>
            {
                users.TryGetValue(diploma.StudentId, out ApplicationUser? student);
                string groupName = MissingLabel;
                if (student?.StudyGroupId is { } groupId
                    && groupNames.TryGetValue(groupId, out string? name))
                {
                    groupName = name;
                }

                string supervisorName = MissingLabel;
                if (diploma.SupervisorId.HasValue
                    && users.TryGetValue(diploma.SupervisorId.Value, out ApplicationUser? supervisor))
                {
                    supervisorName = supervisor.FullName;
                }

                string? reviewerName = null;
                if (diploma.ReviewerId.HasValue
                    && users.TryGetValue(diploma.ReviewerId.Value, out ApplicationUser? reviewer))
                {
                    reviewerName = reviewer.FullName;
                }

                return new AdmittedReportItemDto(
                    student?.FullName ?? MissingLabel,
                    groupName,
                    topicTitles.GetValueOrDefault(diploma.Id, MissingLabel),
                    supervisorName,
                    reviewerName,
                    diploma.DefenceDate!.Value);
            })
            .OrderBy(item => PersonNameSort.SurnameKey(item.StudentFullName), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(item => item.StudentFullName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
