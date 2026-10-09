using DiplomaManagementSystem.Application.Identity;

namespace DiplomaManagementSystem.Application.Common;

public static class EmployeeOrderNameFormatter
{
    public static string Format(ApplicationUser employee)
    {
        string shortName = string.IsNullOrWhiteSpace(employee.ShortDisplayName)
            ? AcademicNameFormatter.ToShortDisplayName(employee.FullName)
            : employee.ShortDisplayName.Trim();

        if (employee.AcademicRank is not { } rank)
        {
            return shortName;
        }

        string abbreviation = AcademicRankLabels.GetAbbreviation(rank);
        return string.IsNullOrEmpty(abbreviation)
            ? shortName
            : $"{abbreviation} {shortName}";
    }
}
