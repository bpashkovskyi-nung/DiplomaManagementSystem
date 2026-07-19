using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;

using Microsoft.AspNetCore.Mvc.Rendering;

namespace DiplomaManagementSystem.Web.Mapping;

internal static class SecretaryDiplomaDetailsMapper
{
    public static DiplomaDetailsViewModel Map(DiplomaDetailsDto details)
    {
        IReadOnlyList<TopicVersionDetailViewModel> topicVersions = details.History.TopicVersions
            .Select(version => TopicVersionDisplayMapper.MapSecretaryTopicVersion(
                version,
                details.Assignments.SupervisorName))
            .ToList();

        return new DiplomaDetailsViewModel
        {
            Id = details.Header.Id,
            SessionId = details.Header.SessionId,
            SessionType = details.Header.SessionType,
            StudentFullName = details.Header.StudentFullName,
            StudentEmail = details.Header.StudentEmail,
            StudyGroupName = details.Header.StudyGroupName,
            SupervisorId = details.Assignments.SupervisorId,
            SupervisorName = details.Assignments.SupervisorName,
            SupervisorAssignmentStatus = details.Assignments.SupervisorAssignmentStatus,
            SupervisorAssignmentDisplay = UkrainianDisplay.FormatSupervisorAssignmentStatus(
                details.Assignments.SupervisorAssignmentStatus),
            ReviewerId = details.Assignments.ReviewerId,
            ReviewerName = details.Assignments.ReviewerName,
            ReviewAssignmentStatus = details.Assignments.ReviewAssignmentStatus,
            ReviewAssignmentDisplay = UkrainianDisplay.FormatReviewAssignmentStatus(
                details.Assignments.ReviewAssignmentStatus),
            LifecycleStatus = details.State.LifecycleStatus,
            LifecycleDisplay = UkrainianDisplay.FormatSecretaryWorkflowStatus(
                details.State.LifecycleStatus,
                details.State.CurrentAdmissionStep),
            AdmissionStatus = details.State.AdmissionStatus,
            AdmissionDisplay = UkrainianDisplay.FormatDiplomaAdmissionStatus(details.State.AdmissionStatus),
            CurrentAdmissionStep = details.State.CurrentAdmissionStep,
            CurrentAdmissionStepDisplay = details.State.CurrentAdmissionStep.HasValue
                ? UkrainianDisplay.FormatAdmissionStep(details.State.CurrentAdmissionStep.Value)
                : null,
            DefenceDate = details.State.DefenceDate,
            AdmissionSteps = details.History.AdmissionSteps
                .Select(step => new AdmissionStepStatusViewModel
                {
                    Step = step.Step,
                    StepDisplay = UkrainianDisplay.FormatAdmissionStep(step.Step),
                    IsPassing = step.IsPassing,
                    OutcomeDisplay = step.Outcome.HasValue
                        ? UkrainianDisplay.FormatCheckpointOutcome(step.Outcome.Value)
                        : null,
                    Comment = step.Comment,
                    RecordedByName = step.RecordedByName,
                    RecordedAt = step.RecordedAt,
                    IsSecretaryOverride = step.IsSecretaryOverride,
                    AttemptCount = step.AttemptCount,
                })
                .ToList(),
            AttemptHistory = details.History.AttemptHistory
                .Select(attempt => new AdmissionStepAttemptViewModel
                {
                    Step = attempt.Step,
                    StepDisplay = UkrainianDisplay.FormatAdmissionStep(attempt.Step),
                    AttemptNumber = attempt.AttemptNumber,
                    OutcomeDisplay = UkrainianDisplay.FormatCheckpointOutcome(attempt.Outcome),
                    Comment = attempt.Comment,
                    RecordedByName = attempt.RecordedByName,
                    RecordedAt = attempt.RecordedAt,
                    IsSecretaryOverride = attempt.IsSecretaryOverride,
                })
                .ToList(),
            TopicVersions = topicVersions,
            TopicHistory = TopicHistoryMapper.Map(topicVersions),
            Comments = details.History.Comments
                .Select(comment => new CommentDetailViewModel
                {
                    AuthorName = comment.AuthorName,
                    Body = comment.Body,
                    CreatedAt = comment.CreatedAt,
                })
                .ToList(),
            CanOverrideSupervisor = details.Actions.CanOverrideSupervisor,
            ShowOverrideSupervisorSection = details.Actions.ShowOverrideSupervisorSection,
            OverrideSupervisorBlockedReason = details.Actions.OverrideSupervisorBlockedReason,
            ShowAssignReviewerSection = details.Actions.ShowAssignReviewerSection,
            CanAssignReviewer = details.Actions.CanAssignReviewer,
            AssignReviewerBlockedReason = details.Actions.AssignReviewerBlockedReason,
            ShowAdmitSection = details.Actions.ShowAdmitSection,
            CanAdmit = details.Actions.CanAdmit,
            AdmitBlockedReason = details.Actions.AdmitBlockedReason,
            ShowOverrideAdmissionStepSection = details.Actions.ShowOverrideAdmissionStepSection,
            CanOverrideAdmissionStep = details.Actions.CanOverrideAdmissionStep,
            OverrideAdmissionStepBlockedReason = details.Actions.OverrideAdmissionStepBlockedReason,
            ShowAddCommentSection = details.Actions.ShowAddCommentSection,
            CanAddComment = details.Actions.CanAddComment,
            AddCommentBlockedReason = details.Actions.AddCommentBlockedReason,
            WorkflowProgress = WorkflowProgressMapper.Map(
                details.WorkflowProgress,
                "Поточний етап:"),
            Documents = DiplomaDocumentMapper.Map(details.Documents),
            EmployeePool = details.EmployeePool
                .Select(employee => new SelectListItem($"{employee.FullName} ({employee.Email})", employee.Id.ToString()))
                .ToList(),
        };
    }
}
