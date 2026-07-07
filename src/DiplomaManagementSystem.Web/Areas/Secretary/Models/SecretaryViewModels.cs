using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Areas.Student.Models;
using DiplomaManagementSystem.Web.Models.Shared;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Areas.Secretary.Models;

public sealed record SecretaryNavLink(string Text, string Controller, string Action);

public sealed class SessionSelectViewModel
{
    public IReadOnlyList<SelectListItem> Sessions { get; set; } = [];

    public Guid? SelectedSessionId { get; set; }
}

public sealed class SessionSelectorViewModel
{
    public Guid? SelectedSessionId { get; set; }

    public IReadOnlyList<SelectListItem> Sessions { get; set; } = [];
}

public sealed class SecretaryDashboardBucketViewModel
{
    public DiplomaLifecycleStatus? LifecycleStatus { get; set; }

    public AdmissionStep? AdmissionStep { get; set; }

    public string Label { get; set; } = string.Empty;

    public string BadgeClass { get; set; } = string.Empty;

    public int Count { get; set; }
}

public sealed class SecretaryDashboardViewModel
{
    public Guid SessionId { get; set; }

    public DefenceSessionType SessionType { get; set; }

    public string SessionLabel { get; set; } = string.Empty;

    public IReadOnlyList<SecretaryDashboardBucketViewModel> Buckets { get; set; } = [];

    public int TotalDiplomas { get; set; }
}

public sealed class DiplomaListViewModel
{
    public Guid SessionId { get; set; }

    public DefenceSessionType SessionType { get; set; }

    public IReadOnlyList<DiplomaListItemViewModel> Items { get; set; } = [];

    public DiplomaListFilterViewModel Filter { get; set; } = new();
}

public sealed class DiplomaListFilterViewModel
{
    public DiplomaLifecycleStatus? LifecycleStatus { get; set; }

    public AdmissionStep? CurrentAdmissionStep { get; set; }

    public Guid? StudyGroupId { get; set; }

    public string? Search { get; set; }

    public IReadOnlyList<SelectListItem> LifecycleStatuses { get; set; } = [];

    public IReadOnlyList<SelectListItem> AdmissionSteps { get; set; } = [];

    public IReadOnlyList<SelectListItem> StudyGroups { get; set; } = [];
}

public sealed class DiplomaListItemViewModel
{
    public Guid Id { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentEmail { get; set; } = string.Empty;

    public string StudyGroupName { get; set; } = string.Empty;

    public string? SupervisorName { get; set; }

    public string? TopicTitle { get; set; }

    public DiplomaLifecycleStatus LifecycleStatus { get; set; }

    public string LifecycleDisplay { get; set; } = string.Empty;

    public DiplomaAdmissionStatus AdmissionStatus { get; set; }

    public AdmissionStep? CurrentAdmissionStep { get; set; }

    public string? CurrentAdmissionStepDisplay { get; set; }

    public int OutcomeStepsCompleted { get; set; }

    public int OutcomeStepsTotal { get; set; }
}

public sealed class LifecycleBadgeViewModel
{
    public string Display { get; set; } = string.Empty;

    public string BadgeClass { get; set; } = "bg-secondary";
}

public sealed class OutcomeStepsChecklistViewModel
{
    public int Completed { get; set; }

    public int Total { get; set; }
}

public sealed class DiplomaDetailsViewModel
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public DefenceSessionType SessionType { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string StudentEmail { get; set; } = string.Empty;

    public string StudyGroupName { get; set; } = string.Empty;

    public Guid? SupervisorId { get; set; }

    public string? SupervisorName { get; set; }

    public SupervisorAssignmentStatus SupervisorAssignmentStatus { get; set; }

    public string SupervisorAssignmentDisplay { get; set; } = string.Empty;

    public Guid? ReviewerId { get; set; }

    public string? ReviewerName { get; set; }

    public ReviewAssignmentStatus ReviewAssignmentStatus { get; set; }

    public string ReviewAssignmentDisplay { get; set; } = string.Empty;

    public DiplomaLifecycleStatus LifecycleStatus { get; set; }

    public string LifecycleDisplay { get; set; } = string.Empty;

    public DiplomaAdmissionStatus AdmissionStatus { get; set; }

    public string AdmissionDisplay { get; set; } = string.Empty;

    public AdmissionStep? CurrentAdmissionStep { get; set; }

    public string? CurrentAdmissionStepDisplay { get; set; }

    public DateOnly? DefenceDate { get; set; }

    public IReadOnlyList<AdmissionStepStatusViewModel> AdmissionSteps { get; set; } = [];

    public IReadOnlyList<AdmissionStepAttemptViewModel> AttemptHistory { get; set; } = [];

    public IReadOnlyList<TopicVersionDetailViewModel> TopicVersions { get; set; } = [];

    public IReadOnlyList<TopicHistoryEntryViewModel> TopicHistory { get; set; } = [];

    public IReadOnlyList<CommentDetailViewModel> Comments { get; set; } = [];

    public bool ShowOverrideSupervisorSection { get; set; }

    public bool CanOverrideSupervisor { get; set; }

    public string? OverrideSupervisorBlockedReason { get; set; }

    public bool ShowAssignReviewerSection { get; set; }

    public bool CanAssignReviewer { get; set; }

    public string? AssignReviewerBlockedReason { get; set; }

    public bool ShowAdmitSection { get; set; }

    public bool CanAdmit { get; set; }

    public string? AdmitBlockedReason { get; set; }

    public bool ShowOverrideAdmissionStepSection { get; set; }

    public bool CanOverrideAdmissionStep { get; set; }

    public string? OverrideAdmissionStepBlockedReason { get; set; }

    public bool ShowAddCommentSection { get; set; }

    public bool CanAddComment { get; set; }

    public string? AddCommentBlockedReason { get; set; }

    public WorkflowProgressViewModel WorkflowProgress { get; set; } = new();

    public DiplomaDocumentsViewModel Documents { get; set; } = new();

    public IReadOnlyList<SelectListItem> EmployeePool { get; set; } = [];
}

public sealed class AdmissionStepStatusViewModel
{
    public AdmissionStep Step { get; set; }

    public string StepDisplay { get; set; } = string.Empty;

    public bool IsPassing { get; set; }

    public string? OutcomeDisplay { get; set; }

    public string? Comment { get; set; }

    public string? RecordedByName { get; set; }

    public DateTimeOffset? RecordedAt { get; set; }

    public bool IsSecretaryOverride { get; set; }

    public int AttemptCount { get; set; }
}

public sealed class AdmissionStepAttemptViewModel
{
    public AdmissionStep Step { get; set; }

    public string StepDisplay { get; set; } = string.Empty;

    public int AttemptNumber { get; set; }

    public string OutcomeDisplay { get; set; } = string.Empty;

    public string? Comment { get; set; }

    public string RecordedByName { get; set; } = string.Empty;

    public DateTimeOffset RecordedAt { get; set; }

    public bool IsSecretaryOverride { get; set; }
}

public sealed class TopicVersionDetailViewModel
{
    public int VersionNumber { get; set; }

    public string Title { get; set; } = string.Empty;

    public string StatusDisplay { get; set; } = string.Empty;

    public string? RejectionReason { get; set; }

    public string SubmittedAtDisplay { get; set; } = string.Empty;

    public string? SupervisorApprovalLine { get; set; }

    public string? HeadApprovalLine { get; set; }

    public string? RejectionLine { get; set; }
}

public sealed class CommentDetailViewModel
{
    public string AuthorName { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class AssignReviewerViewModel
{
    public Guid DiplomaId { get; set; }

    public Guid ReviewerId { get; set; }
}

public sealed class AdmitDiplomaViewModel
{
    public Guid DiplomaId { get; set; }

    public DateOnly DefenceDate { get; set; }
}

public sealed class OverrideSupervisorViewModel
{
    public Guid DiplomaId { get; set; }

    public Guid SupervisorId { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public sealed class AddCommentViewModel
{
    public Guid DiplomaId { get; set; }

    public string Body { get; set; } = string.Empty;
}

public sealed class OverrideAdmissionStepViewModel
{
    public Guid DiplomaId { get; set; }

    public AdmissionStep Step { get; set; }

    public CheckpointOutcome Outcome { get; set; }

    public string Comment { get; set; } = string.Empty;
}

public sealed class AdmittedReportViewModel
{
    public Guid SessionId { get; set; }

    public string SessionLabel { get; set; } = string.Empty;

    public IReadOnlyList<AdmittedReportItemViewModel> Items { get; set; } = [];
}

public sealed class AdmittedReportItemViewModel
{
    public string StudentFullName { get; set; } = string.Empty;

    public string StudyGroupName { get; set; } = string.Empty;

    public string TopicTitle { get; set; } = string.Empty;

    public string SupervisorName { get; set; } = string.Empty;

    public string? ReviewerName { get; set; }

    public DateOnly? DefenceDate { get; set; }
}

public sealed class TopicOrderStudyGroupOptionViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? Course { get; set; }
}

public sealed class TopicOrderViewModel
{
    public Guid SessionId { get; set; }

    public string SessionLabel { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    public int Year { get; set; }

    public IReadOnlyList<TopicOrderStudyGroupOptionViewModel> StudyGroups { get; set; } = [];

    public List<Guid> SelectedStudyGroupIds { get; set; } = [];

    public IReadOnlyList<string> Warnings { get; set; } = [];

    public int? PreviewStudentCount { get; set; }
}
