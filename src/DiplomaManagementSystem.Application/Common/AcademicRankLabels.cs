using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Common;

public static class AcademicRankLabels
{
    public static string GetAbbreviation(EmployeeAcademicRank rank) => rank switch
    {
        EmployeeAcademicRank.Assistant => "асист.",
        EmployeeAcademicRank.Lecturer => "викладач",
        EmployeeAcademicRank.SeniorLecturer => "ст. викладач",
        EmployeeAcademicRank.AssociateProfessor => "доц.",
        EmployeeAcademicRank.Professor => "проф.",
        _ => string.Empty,
    };

    public static string GetDisplayName(EmployeeAcademicRank rank) => rank switch
    {
        EmployeeAcademicRank.Assistant => "Асистент",
        EmployeeAcademicRank.Lecturer => "Викладач",
        EmployeeAcademicRank.SeniorLecturer => "Старший викладач",
        EmployeeAcademicRank.AssociateProfessor => "Доцент",
        EmployeeAcademicRank.Professor => "Професор",
        _ => string.Empty,
    };

    public static bool TryParse(string? value, out EmployeeAcademicRank rank)
    {
        rank = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim().ToLowerInvariant();
        rank = normalized switch
        {
            "assistant" or "асистент" or "асист." or "асист" => EmployeeAcademicRank.Assistant,
            "lecturer" or "викладач" => EmployeeAcademicRank.Lecturer,
            "seniorlecturer" or "старший викладач" or "ст. викладач" or "ст викладач" => EmployeeAcademicRank.SeniorLecturer,
            "associateprofessor" or "доцент" or "доц." or "доц" => EmployeeAcademicRank.AssociateProfessor,
            "professor" or "професор" or "проф." or "проф" => EmployeeAcademicRank.Professor,
            _ => default,
        };

        return normalized is "assistant" or "асистент" or "асист." or "асист"
               or "lecturer" or "викладач"
               or "seniorlecturer" or "старший викладач" or "ст. викладач" or "ст викладач"
               or "associateprofessor" or "доцент" or "доц." or "доц"
               or "professor" or "професор" or "проф." or "проф";
    }
}
