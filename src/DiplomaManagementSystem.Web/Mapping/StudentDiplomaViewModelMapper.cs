using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;
using DiplomaManagementSystem.Web.Areas.Student.Models;
using DiplomaManagementSystem.Web.Models.Shared;

using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Mapping;

internal static class StudentDiplomaViewModelMapper
{
    public static MyDiplomaViewModel Map(MyDiplomaDto dto)
    {
        IReadOnlyList<TopicVersionDetailViewModel> topicDetailVersions = dto.History.TopicVersions
            .Select(version => TopicVersionDisplayMapper.MapStudentTopicVersion(version, dto.Assignments.SupervisorName))
            .ToList();

        return new MyDiplomaViewModel
        {
            DiplomaId = dto.Header.DiplomaId,
            HasDiploma = dto.Header.HasDiploma,
            SessionType = dto.Header.SessionType,
            SessionLabel = dto.Header.SessionLabel,
            SupervisorName = dto.Assignments.SupervisorName,
            SupervisorAssignmentStatus = dto.Assignments.SupervisorAssignmentStatus,
            SupervisorAssignmentDisplay = dto.Assignments.SupervisorAssignmentStatus.HasValue
                ? UkrainianDisplay.FormatSupervisorAssignmentStatus(
                    dto.Assignments.SupervisorAssignmentStatus.Value,
                    dto.Assignments.SupervisorId)
                : null,
            LifecycleStatus = dto.State?.LifecycleStatus,
            LifecycleDisplay = dto.State?.LifecycleStatus is { } lifecycleStatus
                ? UkrainianDisplay.FormatDiplomaLifecycleStatus(lifecycleStatus)
                : null,
            CurrentTopicTitle = dto.Assignments.CurrentTopicTitle,
            TopicStatus = dto.Assignments.TopicStatus,
            TopicStatusDisplay = dto.Assignments.TopicStatus.HasValue
                ? UkrainianDisplay.FormatTopicVersionStatus(dto.Assignments.TopicStatus.Value)
                : null,
            AdmissionSteps = dto.History.Checkpoints
                .Select(step => new StudentAdmissionStepViewModel
                {
                    Step = step.Step,
                    StepDisplay = UkrainianDisplay.FormatAdmissionStep(step.Step),
                    IsPassing = step.IsPassing,
                    IsCurrent = step.IsCurrent,
                    IsLocked = step.IsLocked,
                })
                .ToList(),
            TopicVersions = dto.History.TopicVersions
                .Select(version => new TopicVersionItemViewModel
                {
                    VersionId = version.VersionId,
                    VersionNumber = version.VersionNumber,
                    Title = version.Title,
                    Status = version.Status,
                    StatusDisplay = UkrainianDisplay.FormatTopicVersionStatus(version.Status),
                    RejectionReason = version.RejectionReason,
                    SubmittedAt = version.SubmittedAt,
                    ReviewedAt = version.ReviewedAt,
                })
                .ToList(),
            TopicHistory = TopicHistoryMapper.Map(topicDetailVersions),
            Comments = dto.History.Comments
                .Select(comment => new CommentItemViewModel
                {
                    AuthorName = comment.AuthorName,
                    Body = comment.Body,
                    CreatedAt = comment.CreatedAt,
                })
                .ToList(),
            CanSelectSupervisor = dto.Actions?.CanSelectSupervisor ?? false,
            ShowSupervisorSection = dto.Actions?.ShowSupervisorSection ?? false,
            SelectSupervisorBlockedReason = dto.Actions?.SelectSupervisorBlockedReason,
            CanSubmitTopic = dto.Actions?.CanSubmitTopic ?? false,
            ShowTopicSubmissionSection = dto.Actions?.ShowTopicSubmissionSection ?? false,
            SubmitTopicBlockedReason = dto.Actions?.SubmitTopicBlockedReason,
            ShowCheckpointsSection = dto.Actions?.ShowCheckpointsSection ?? false,
            ShowWorkReadinessSection = dto.Actions?.ShowWorkReadinessSection ?? false,
            CanDeclareWorkReady = dto.Actions?.CanDeclareWorkReady ?? false,
            DeclareWorkReadyBlockedReason = dto.Actions?.DeclareWorkReadyBlockedReason,
            ShowWorkUploadSection = dto.Actions?.ShowWorkUploadSection ?? false,
            CanUploadWork = dto.Actions?.CanUploadWork ?? false,
            UploadWorkBlockedReason = dto.Actions?.UploadWorkBlockedReason,
            Documents = DiplomaDocumentMapper.Map(dto.Documents),
            WorkflowProgress = dto.WorkflowProgress is null
                ? null
                : WorkflowProgressMapper.Map(dto.WorkflowProgress),
            SupervisorPool = dto.SupervisorPool
                .Select(option => new SelectListItem($"{option.FullName} ({option.Email})", option.Id.ToString()))
                .ToList(),
        };
    }
}
