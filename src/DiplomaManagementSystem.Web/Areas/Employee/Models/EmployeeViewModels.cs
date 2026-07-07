namespace DiplomaManagementSystem.Web.Areas.Employee.Models;

using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;
using DiplomaManagementSystem.Web.Models.Shared;

using Microsoft.AspNetCore.Http;

public sealed class EmployeeHomeViewModel
{
    public IReadOnlyList<EmployeeRoleCardViewModel> Roles { get; set; } = [];
}

public sealed class EmployeeRoleCardViewModel
{
    public string RoleKey { get; set; } = string.Empty;

    public string RoleDisplay { get; set; } = string.Empty;

    public int PendingCount { get; set; }

    public string Controller { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public bool CountsStudents { get; set; }
}

public sealed class PendingStudentsViewModel
{
    public IReadOnlyList<SupervisorStudentItemViewModel> Items { get; set; } = [];
}

public sealed class SupervisorStudentItemViewModel
{
    public Guid DiplomaId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string StudyGroupName { get; set; } = string.Empty;

    public DateTimeOffset RequestedAt { get; set; }
}

public sealed class SupervisorActionViewModel
{
    public Guid DiplomaId { get; set; }

    public string? Comment { get; set; }
}

public sealed class SupervisorStudentsListViewModel
{
    public IReadOnlyList<DiplomaListItemViewModel> Items { get; set; } = [];

    public DiplomaListFilterViewModel Filter { get; set; } = new();

    public bool ShowSupervisorColumn { get; set; }

    public bool ShowDetailsLink { get; set; }
}

public sealed class TopicReviewsViewModel
{
    public IReadOnlyList<TopicReviewItemViewModel> Items { get; set; } = [];
}

public sealed class TopicReviewItemViewModel
{
    public Guid VersionId { get; set; }

    public Guid DiplomaId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string? SupervisorFullName { get; set; }

    public string Title { get; set; } = string.Empty;

    public int VersionNumber { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }
}

public sealed class ApproveTopicViewModel
{
    public Guid VersionId { get; set; }

    public string? Comment { get; set; }
}

public sealed class ReviewTopicViewModel
{
    public Guid VersionId { get; set; }

    public string? RejectionReason { get; set; }
}

public sealed class TopicReviewTableViewModel
{
    public IReadOnlyList<TopicReviewItemViewModel> Items { get; set; } = [];

    public string ApproveAction { get; set; } = "ApproveTopic";

    public string RejectAction { get; set; } = "RejectTopic";

    public string ApproveButtonText { get; set; } = "Погодити";

    public string EmptyMessage { get; set; } = "Немає тем на розгляді.";

    public bool ShowSupervisorColumn { get; set; }
}

public sealed class PendingTopicsViewModel
{
    public IReadOnlyList<TopicReviewItemViewModel> Items { get; set; } = [];
}

public sealed class PendingCheckpointsViewModel
{
    public string Title { get; set; } = string.Empty;

    public IReadOnlyList<PendingCheckpointItemViewModel> Items { get; set; } = [];

    public string FormAction { get; set; } = "Complete";

    public bool RequiresDocumentFile { get; set; } = true;

    public string EmptyMessage { get; set; } = "Немає очікуючих перевірок.";

    public IReadOnlyList<EmployeeNavLink> NavLinks { get; set; } = [];
}

public sealed record EmployeeNavLink(string Text, string Controller, string Action);

public sealed class PendingCheckpointListViewModel
{
    public IReadOnlyList<PendingCheckpointItemViewModel> Items { get; set; } = [];

    public string FormAction { get; set; } = "Complete";

    public bool RequiresDocumentFile { get; set; } = true;

    public string EmptyMessage { get; set; } = "Немає очікуючих перевірок.";
}

public sealed class PendingCheckpointItemViewModel
{
    public Guid DiplomaId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string StudyGroupName { get; set; } = string.Empty;

    public string? TopicTitle { get; set; }

    public PendingStudentWorkLinkViewModel? LatestStudentWork { get; set; }
}

public sealed class PendingStudentWorkLinkViewModel
{
    public string FileName { get; set; } = string.Empty;

    public string ViewUrl { get; set; } = string.Empty;

    public int VersionNumber { get; set; }
}

public sealed class CompleteCheckpointViewModel
{
    public Guid DiplomaId { get; set; }

    public CheckpointOutcome Outcome { get; set; }

    public string? Comment { get; set; }

    public IFormFile? Document { get; set; }

    public bool RequiresDocumentFile { get; set; } = true;
}

public sealed class CheckpointReviewActionsViewModel
{
    public Guid DiplomaId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string FormAction { get; set; } = "Complete";

    public bool RequiresDocumentFile { get; set; } = true;
}
