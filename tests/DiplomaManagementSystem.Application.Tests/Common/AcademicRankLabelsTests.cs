using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests.Common;

public sealed class AcademicRankLabelsTests
{
    [Theory]
    [InlineData(EmployeeAcademicRank.AssociateProfessor, "доц.")]
    [InlineData(EmployeeAcademicRank.Assistant, "асист.")]
    [InlineData(EmployeeAcademicRank.Professor, "проф.")]
    public void GetAbbreviation_ReturnsExpected(EmployeeAcademicRank rank, string expected)
    {
        Assert.Equal(expected, AcademicRankLabels.GetAbbreviation(rank));
    }

    [Theory]
    [InlineData("доцент", EmployeeAcademicRank.AssociateProfessor)]
    [InlineData("асист.", EmployeeAcademicRank.Assistant)]
    [InlineData("професор", EmployeeAcademicRank.Professor)]
    public void TryParse_ParsesUkrainianValues(string raw, EmployeeAcademicRank expected)
    {
        Assert.True(AcademicRankLabels.TryParse(raw, out EmployeeAcademicRank rank));
        Assert.Equal(expected, rank);
    }
}
