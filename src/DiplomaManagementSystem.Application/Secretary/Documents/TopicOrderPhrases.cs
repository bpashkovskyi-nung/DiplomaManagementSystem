using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Secretary.Documents;

internal static class TopicOrderPhrases
{
    public static string FormatSessionLevelPhrase(DefenceSessionType sessionType) => sessionType switch
    {
        DefenceSessionType.Bachelor => "першого бакалаврського рівня вищої освіти",
        DefenceSessionType.Master => "другого (магістерського) рівня вищої освіти",
        _ => string.Empty,
    };

    public static string FormatGroupsPhrase(IReadOnlyList<string> groupNames) =>
        string.Join(',', groupNames);

    public static string FormatCoursePhrase(int course) =>
        $"{CourseUkrainianLabel.FormatGenitive(course)} курсу";
}
