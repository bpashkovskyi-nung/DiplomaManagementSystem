using System.Globalization;
using System.Resources;

using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Resources;

namespace DiplomaManagementSystem.Web;

public static class UkrainianDisplay
{
    private static readonly ResourceManager ResourceManager = new(
        "DiplomaManagementSystem.Web.Resources.DisplayResources",
        typeof(DisplayResources).Assembly);

    public static string FormatEnum<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        Get($"{typeof(TEnum).Name}_{value}");

    public static string FormatDefenceSessionType(DefenceSessionType type) =>
        FormatEnum(type);

    public static string FormatDefenceSessionStatus(DefenceSessionStatus status) =>
        FormatEnum(status);

    public static string FormatAnnualRoleType(AnnualRoleType role) =>
        FormatEnum(role);

    public static string FormatDiplomaLifecycleStatus(DiplomaLifecycleStatus status) =>
        FormatEnum(status);

    public static string FormatDiplomaAdmissionStatus(DiplomaAdmissionStatus status) =>
        FormatEnum(status);

    public static string FormatSupervisorAssignmentStatus(
        SupervisorAssignmentStatus status,
        Guid? supervisorId = null)
    {
        if (!SupervisorAssignmentRules.HasPendingRequest(status, supervisorId)
            && status == SupervisorAssignmentStatus.Pending)
        {
            return Get("SupervisorAssignmentStatus_NotSelected");
        }

        return FormatEnum(status);
    }

    public static string FormatTopicVersionStatus(TopicVersionStatus status) =>
        FormatEnum(status);

    public static string FormatAdmissionStep(AdmissionStep step) =>
        WorkflowUkrainianLabels.FormatAdmissionStep(step);

    public static string SecretaryOverrideBadgeLabel =>
        WorkflowUkrainianLabels.SecretaryOverrideBadge;

    public static string FormatSecretaryWorkflowStatus(
        DiplomaLifecycleStatus lifecycleStatus,
        AdmissionStep? currentAdmissionStep) =>
        lifecycleStatus == DiplomaLifecycleStatus.DocumentsInProgress && currentAdmissionStep is AdmissionStep step
            ? FormatAdmissionStep(step)
            : FormatDiplomaLifecycleStatus(lifecycleStatus);

    public static string SecretaryWorkflowBadgeClass(
        DiplomaLifecycleStatus lifecycleStatus,
        AdmissionStep? currentAdmissionStep) =>
        lifecycleStatus == DiplomaLifecycleStatus.DocumentsInProgress && currentAdmissionStep is AdmissionStep step
            ? AdmissionStepBadgeClass(step)
            : LifecycleBadgeClass(lifecycleStatus);

    public static string FormatCheckpointOutcome(CheckpointOutcome outcome) =>
        FormatEnum(outcome);

    public static string FormatReviewAssignmentStatus(ReviewAssignmentStatus status) =>
        FormatEnum(status);

    public static string FormatDiplomaDocumentKind(DiplomaDocumentKind kind) =>
        FormatEnum(kind);

    public static string FormattingReviewDocumentStatusHint =>
        Get("DiplomaDocuments_FormattingReviewStatusOnly");

    public static string AdmissionStepBadgeClass(AdmissionStep step) => "bg-info text-dark";

    public static string LifecycleBadgeClass(DiplomaLifecycleStatus status) => status switch
    {
        DiplomaLifecycleStatus.AwaitingSupervisor => "bg-secondary",
        DiplomaLifecycleStatus.SupervisorConfirmed => "bg-info",
        DiplomaLifecycleStatus.TopicInReview => "bg-warning text-dark",
        DiplomaLifecycleStatus.TopicApproved => "bg-primary",
        DiplomaLifecycleStatus.WorkInProgressByStudent => "bg-primary",
        DiplomaLifecycleStatus.DocumentsInProgress => "bg-info",
        DiplomaLifecycleStatus.ReadyForAdmission => "bg-success",
        DiplomaLifecycleStatus.Admitted => "bg-dark",
        _ => "bg-secondary",
    };

    private static string Get(string key)
    {
        string? value = ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        return value ?? key;
    }
}
