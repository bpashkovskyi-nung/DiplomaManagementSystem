using System.Text;

using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Entities;

namespace DiplomaManagementSystem.Application.Documents;

public static class DiplomaStoragePathBuilder
{
    public static IReadOnlyList<string> BuildFolderSegments(
        DefenceSession session,
        StudyGroup studyGroup,
        ApplicationUser student)
    {
        string yearSegment = session.Year.ToString();
        string sessionSegment = SanitizeSegment(
            SecretarySessionLabel.Format(session.Year, session.Type, session.Semester));
        string groupSegment = SanitizeSegment(studyGroup.Name);
        string studentSegment = SanitizeSegment(student.FullName);

        return [yearSegment, sessionSegment, groupSegment, studentSegment];
    }

    public static string SanitizeSegment(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        StringBuilder builder = new(value.Length);
        foreach (char character in value.Trim())
        {
            if (character is '/' or '\\' or ':' or '*' or '?' or '"' or '<' or '>' or '|')
            {
                builder.Append('_');
                continue;
            }

            builder.Append(character);
        }

        string sanitized = builder.ToString().Trim().TrimEnd('.');
        return sanitized.Length == 0 ? "unknown" : sanitized;
    }
}
