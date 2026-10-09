using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application;

public static class WorkflowUkrainianLabels
{
    public const string SecretaryOverrideBadge = "Примусово";

    public const string SecretaryOverrideAdmissionStepSection = "Примусова зміна кроку допуску";

    public const string SecretaryOverrideApplyButton = "Застосувати зміну";

    public const string SecretaryOverrideArchivedBlocked = "Сесія заархівована — примусова зміна недоступна.";

    public const string SupervisorChangeCommentPrefix = "[Зміна керівника секретарем]";

    public static string BuildAdmissionStepOverrideCommentPrefix(AdmissionStep step) =>
        $"[Примусова зміна секретарем: {FormatAdmissionStep(step)}]";

    public static string BuildAdmissionStepOverrideAuditNewValue(
        AdmissionStep step,
        CheckpointOutcome outcome,
        string comment) =>
        $"Секретар ДЕК: {FormatAdmissionStep(step)} → {FormatCheckpointOutcome(outcome)}; {comment.Trim()}";

    public static string FormatCheckpointOutcome(CheckpointOutcome outcome) => outcome switch
    {
        CheckpointOutcome.Approved => "Допущено",
        CheckpointOutcome.NotApproved => "Не допущено",
        _ => outcome.ToString(),
    };

    public static string FormatAdmissionStep(AdmissionStep step) => step switch
    {
        AdmissionStep.SupervisorFeedback => "Відгук керівника",
        AdmissionStep.FormattingReview => "Нормоконтроль",
        AdmissionStep.AntiPlagiarismClearance => "Антиплагіат",
        AdmissionStep.ReviewerAssignment => "Призначення рецензента",
        AdmissionStep.ExternalReview => "Рецензія",
        _ => step.ToString(),
    };

    public static string FormatAdmissionStepInline(AdmissionStep step) => step switch
    {
        AdmissionStep.SupervisorFeedback => "відгук керівника",
        AdmissionStep.FormattingReview => "нормоконтроль",
        AdmissionStep.AntiPlagiarismClearance => "антиплагіат",
        AdmissionStep.ReviewerAssignment => "призначення рецензента",
        AdmissionStep.ExternalReview => "рецензія",
        _ => step.ToString(),
    };

    public static string FormatAnnualRoleType(AnnualRoleType roleType) => roleType switch
    {
        AnnualRoleType.DepartmentHead => "Завідувач кафедри",
        AnnualRoleType.ExamCommissionSecretary => "Секретар ДЕК",
        AnnualRoleType.AntiPlagiarismOfficer => "Відповідальний за антиплагіат",
        AnnualRoleType.FormattingReviewer => "Нормоконтролер",
        _ => roleType.ToString(),
    };

    public static string FormatExaminationCommissionRole(ExaminationCommissionRole role) => role switch
    {
        ExaminationCommissionRole.Chair => "Голова ЕК",
        ExaminationCommissionRole.Member => "Член ЕК",
        _ => role.ToString(),
    };
}
