using System.Globalization;

using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Web.Tests;

public sealed class DisplayLocalizationTests
{
    public DisplayLocalizationTests()
    {
        CultureInfo.CurrentUICulture = new CultureInfo("uk-UA");
    }

    [Fact]
    public void UkrainianDisplay_FormatsAnnualRoleTypes()
    {
        Assert.Equal("Завідувач кафедри", UkrainianDisplay.FormatAnnualRoleType(AnnualRoleType.DepartmentHead));
        Assert.Equal("Секретар ДЕК", UkrainianDisplay.FormatAnnualRoleType(AnnualRoleType.ExamCommissionSecretary));
        Assert.Equal("Відповідальний за антиплагіат", UkrainianDisplay.FormatAnnualRoleType(AnnualRoleType.AntiPlagiarismOfficer));
        Assert.Equal("Нормоконтролер", UkrainianDisplay.FormatAnnualRoleType(AnnualRoleType.FormattingReviewer));
    }

    [Fact]
    public void UkrainianDisplay_FormatsOtherEnumLabels()
    {
        Assert.Equal("Тема затверджена", UkrainianDisplay.FormatDiplomaLifecycleStatus(DiplomaLifecycleStatus.TopicApproved));
        Assert.Equal("Рецензента призначено", UkrainianDisplay.FormatDiplomaLifecycleStatus(DiplomaLifecycleStatus.ReviewerAssigned));
        Assert.Equal("Готово до допуску", UkrainianDisplay.FormatDiplomaLifecycleStatus(DiplomaLifecycleStatus.ReadyForAdmission));
        Assert.Equal("Нормоконтроль", UkrainianDisplay.FormatAdmissionStep(AdmissionStep.FormattingReview));
        Assert.Equal("Бакалавр", UkrainianDisplay.FormatDefenceSessionType(DefenceSessionType.Bachelor));
        Assert.Equal("Робота студента", UkrainianDisplay.FormatDiplomaDocumentKind(DiplomaDocumentKind.StudentWork));
    }
}
