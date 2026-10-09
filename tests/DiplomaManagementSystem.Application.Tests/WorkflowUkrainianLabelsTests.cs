using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Tests;

public sealed class WorkflowUkrainianLabelsTests
{
    // TC-APP-HLP-008
    [Theory]
    [InlineData(AdmissionStep.SupervisorFeedback, "Відгук керівника")]
    [InlineData(AdmissionStep.FormattingReview, "Нормоконтроль")]
    [InlineData(AdmissionStep.ExternalReview, "Рецензія")]
    public void FormatAdmissionStep_ReturnsUkrainian(AdmissionStep step, string expected)
    {
        Assert.Equal(expected, WorkflowUkrainianLabels.FormatAdmissionStep(step));
    }

    // TC-APP-HLP-009
    [Theory]
    [InlineData(CheckpointOutcome.Approved, "Допущено")]
    [InlineData(CheckpointOutcome.NotApproved, "Не допущено")]
    public void FormatCheckpointOutcome_ReturnsUkrainian(CheckpointOutcome outcome, string expected)
    {
        Assert.Equal(expected, WorkflowUkrainianLabels.FormatCheckpointOutcome(outcome));
    }

    // TC-APP-HLP-010
    [Fact]
    public void BuildAdmissionStepOverrideCommentPrefix_ContainsStep()
    {
        string prefix = WorkflowUkrainianLabels.BuildAdmissionStepOverrideCommentPrefix(
            AdmissionStep.AntiPlagiarismClearance);

        Assert.Contains("антиплагіат", prefix, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Примусова", prefix, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(AdmissionStep.SupervisorFeedback, "відгук керівника")]
    [InlineData(AdmissionStep.ReviewerAssignment, "призначення рецензента")]
    public void FormatAdmissionStepInline_ReturnsLowercasePhrase(AdmissionStep step, string expected)
    {
        Assert.Equal(expected, WorkflowUkrainianLabels.FormatAdmissionStepInline(step));
    }

    [Theory]
    [InlineData(AdmissionStep.FormattingReview, "Нормоконтроль")]
    [InlineData(AdmissionStep.AntiPlagiarismClearance, "Антиплагіат")]
    [InlineData(AdmissionStep.ReviewerAssignment, "Призначення рецензента")]
    public void FormatAdmissionStep_CoversRemainingSteps(AdmissionStep step, string expected)
    {
        Assert.Equal(expected, WorkflowUkrainianLabels.FormatAdmissionStep(step));
    }

    [Fact]
    public void FormatAdmissionStep_UnknownStep_ReturnsStringValue()
    {
        Assert.Equal("99", WorkflowUkrainianLabels.FormatAdmissionStep((AdmissionStep)99));
    }

    [Fact]
    public void FormatCheckpointOutcome_UnknownOutcome_ReturnsStringValue()
    {
        Assert.Equal("99", WorkflowUkrainianLabels.FormatCheckpointOutcome((CheckpointOutcome)99));
    }

    [Theory]
    [InlineData(AnnualRoleType.DepartmentHead, "Завідувач кафедри")]
    [InlineData(AnnualRoleType.ExamCommissionSecretary, "Секретар ДЕК")]
    [InlineData(AnnualRoleType.AntiPlagiarismOfficer, "Відповідальний за антиплагіат")]
    [InlineData(AnnualRoleType.FormattingReviewer, "Нормоконтролер")]
    public void FormatAnnualRoleType_ReturnsUkrainian(AnnualRoleType roleType, string expected)
    {
        Assert.Equal(expected, WorkflowUkrainianLabels.FormatAnnualRoleType(roleType));
    }

    [Theory]
    [InlineData(ExaminationCommissionRole.Chair, "Голова ЕК")]
    [InlineData(ExaminationCommissionRole.Member, "Член ЕК")]
    public void FormatExaminationCommissionRole_ReturnsUkrainian(ExaminationCommissionRole role, string expected)
    {
        Assert.Equal(expected, WorkflowUkrainianLabels.FormatExaminationCommissionRole(role));
    }

    [Fact]
    public void FormatAdmissionStepInline_UnknownStep_ReturnsStringValue()
    {
        Assert.Equal("99", WorkflowUkrainianLabels.FormatAdmissionStepInline((AdmissionStep)99));
    }

    [Fact]
    public void FormatAnnualRoleType_UnknownRole_ReturnsStringValue()
    {
        Assert.Equal("99", WorkflowUkrainianLabels.FormatAnnualRoleType((AnnualRoleType)99));
    }

    [Fact]
    public void BuildAdmissionStepOverrideAuditNewValue_FormatsStepOutcomeAndComment()
    {
        string value = WorkflowUkrainianLabels.BuildAdmissionStepOverrideAuditNewValue(
            AdmissionStep.FormattingReview,
            CheckpointOutcome.Approved,
            "  Примітка  ");

        Assert.Contains("Нормоконтроль", value, StringComparison.Ordinal);
        Assert.Contains("Допущено", value, StringComparison.Ordinal);
        Assert.Contains("Примітка", value, StringComparison.Ordinal);
        Assert.DoesNotContain("  ", value, StringComparison.Ordinal);
    }
}
