using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests.Common;

public sealed class AcademicRankLabelsTests
{
    [Theory]
    [InlineData(EmployeeAcademicRank.Assistant, "асист.")]
    [InlineData(EmployeeAcademicRank.Lecturer, "викладач")]
    [InlineData(EmployeeAcademicRank.SeniorLecturer, "ст. викладач")]
    [InlineData(EmployeeAcademicRank.AssociateProfessor, "доц.")]
    [InlineData(EmployeeAcademicRank.Professor, "проф.")]
    public void GetAbbreviation_ReturnsExpected(EmployeeAcademicRank rank, string expected)
    {
        Assert.Equal(expected, AcademicRankLabels.GetAbbreviation(rank));
    }

    [Theory]
    [InlineData(EmployeeAcademicRank.Assistant, "Асистент")]
    [InlineData(EmployeeAcademicRank.Lecturer, "Викладач")]
    [InlineData(EmployeeAcademicRank.SeniorLecturer, "Старший викладач")]
    [InlineData(EmployeeAcademicRank.AssociateProfessor, "Доцент")]
    [InlineData(EmployeeAcademicRank.Professor, "Професор")]
    public void GetDisplayName_ReturnsExpected(EmployeeAcademicRank rank, string expected)
    {
        Assert.Equal(expected, AcademicRankLabels.GetDisplayName(rank));
    }

    [Theory]
    [InlineData("асистент", EmployeeAcademicRank.Assistant)]
    [InlineData("доц.", EmployeeAcademicRank.AssociateProfessor)]
    [InlineData("професор", EmployeeAcademicRank.Professor)]
    [InlineData("ст. викладач", EmployeeAcademicRank.SeniorLecturer)]
    [InlineData("lecturer", EmployeeAcademicRank.Lecturer)]
    public void TryParse_WhenKnownValue_ReturnsTrue(string value, EmployeeAcademicRank expected)
    {
        bool parsed = AcademicRankLabels.TryParse(value, out EmployeeAcademicRank rank);

        Assert.True(parsed);
        Assert.Equal(expected, rank);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("unknown")]
    public void TryParse_WhenUnknownValue_ReturnsFalse(string? value)
    {
        bool parsed = AcademicRankLabels.TryParse(value, out EmployeeAcademicRank rank);

        Assert.False(parsed);
        Assert.Equal(default, rank);
    }
}
