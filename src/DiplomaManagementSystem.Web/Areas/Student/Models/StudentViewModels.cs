using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Web.Models.Shared;

using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Areas.Student.Models;

public sealed class MyDiplomaViewModel
{
    public Guid? DiplomaId { get; set; }

    public bool HasDiploma { get; set; }

    public DefenceSessionType? SessionType { get; set; }

    public string? SessionLabel { get; set; }

    public string? SupervisorName { get; set; }

    public SupervisorAssignmentStatus? SupervisorAssignmentStatus { get; set; }

    public string? SupervisorAssignmentDisplay { get; set; }

    public DiplomaLifecycleStatus? LifecycleStatus { get; set; }

    public string? LifecycleDisplay { get; set; }

    public string? CurrentTopicTitle { get; set; }

    public TopicVersionStatus? TopicStatus { get; set; }

    public string? TopicStatusDisplay { get; set; }

    public IReadOnlyList<StudentAdmissionStepViewModel> AdmissionSteps { get; set; } = [];

    public IReadOnlyList<TopicVersionItemViewModel> TopicVersions { get; set; } = [];

    public IReadOnlyList<TopicHistoryEntryViewModel> TopicHistory { get; set; } = [];

    public IReadOnlyList<CommentItemViewModel> Comments { get; set; } = [];

    public bool ShowSupervisorSection { get; set; }

    public bool CanSelectSupervisor { get; set; }

    public string? SelectSupervisorBlockedReason { get; set; }

    public bool ShowTopicSubmissionSection { get; set; }

    public bool CanSubmitTopic { get; set; }

    public string? SubmitTopicBlockedReason { get; set; }

    public bool ShowCheckpointsSection { get; set; }

    public bool ShowWorkReadinessSection { get; set; }

    public bool CanDeclareWorkReady { get; set; }

    public string? DeclareWorkReadyBlockedReason { get; set; }

    public bool ShowWorkUploadSection { get; set; }

    public bool CanUploadWork { get; set; }

    public string? UploadWorkBlockedReason { get; set; }

    public DiplomaDocumentsViewModel? Documents { get; set; }

    public WorkflowProgressViewModel? WorkflowProgress { get; set; }

    public IReadOnlyList<SelectListItem> SupervisorPool { get; set; } = [];

    public Guid? SelectedSupervisorId { get; set; }

    public DefenceDateRequestFormDto? DefenceDateRequest { get; set; }
}

public sealed class SelectSupervisorViewModel
{
    public Guid DiplomaId { get; set; }

    public Guid SupervisorId { get; set; }
}

public sealed class SubmitTopicViewModel
{
    public Guid DiplomaId { get; set; }

    public string Title { get; set; } = string.Empty;
}

public sealed class UploadWorkViewModel
{
    public Guid DiplomaId { get; set; }

    public IFormFile? WorkFile { get; set; }
}

public sealed class DiplomaDocumentsViewModel
{
    public IReadOnlyList<DiplomaDocumentItemViewModel> StudentWorkVersions { get; set; } = [];

    public DiplomaDocumentItemViewModel? LatestSupervisorFeedback { get; set; }

    public DiplomaDocumentItemViewModel? LatestExternalReview { get; set; }

    public DiplomaDocumentItemViewModel? LatestAntiPlagiarismReport { get; set; }
}

public sealed class DiplomaDocumentItemViewModel
{
    public Guid Id { get; set; }

    public DiplomaDocumentKind Kind { get; set; }

    public string KindDisplay { get; set; } = string.Empty;

    public int VersionNumber { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string ViewUrl { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTimeOffset UploadedAt { get; set; }
}
