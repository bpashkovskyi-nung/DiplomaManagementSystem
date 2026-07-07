using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Web.Areas.Employee.Models;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;

namespace DiplomaManagementSystem.Web.Mapping;

internal static class EmployeeViewModelMapper
{
    public static SupervisorStudentItemViewModel MapPendingStudent(PendingStudentDto item) => new()
    {
        DiplomaId = item.DiplomaId,
        StudentFullName = item.StudentFullName,
        StudyGroupName = item.StudyGroupName,
        RequestedAt = item.RequestedAt,
    };

    public static TopicReviewItemViewModel MapTopicReviewItem(TopicReviewItemDto item) => new()
    {
        VersionId = item.VersionId,
        DiplomaId = item.DiplomaId,
        StudentFullName = item.StudentFullName,
        SupervisorFullName = item.SupervisorFullName,
        Title = item.Title,
        VersionNumber = item.VersionNumber,
        SubmittedAt = item.SubmittedAt,
    };

    public static PendingCheckpointItemViewModel MapPendingCheckpoint(PendingCheckpointItemDto item) => new()
    {
        DiplomaId = item.DiplomaId,
        StudentFullName = item.StudentFullName,
        StudyGroupName = item.StudyGroupName,
        TopicTitle = item.TopicTitle,
        LatestStudentWork = MapStudentWorkLink(item.LatestStudentWork),
    };

    public static PendingCheckpointItemViewModel MapReviewerAssignment(ReviewerAssignmentItemDto item) => new()
    {
        DiplomaId = item.DiplomaId,
        StudentFullName = item.StudentFullName,
        StudyGroupName = item.StudyGroupName,
        TopicTitle = item.TopicTitle,
        LatestStudentWork = MapStudentWorkLink(item.LatestStudentWork),
    };

    private static PendingStudentWorkLinkViewModel? MapStudentWorkLink(
        Application.Employee.Dtos.PendingStudentWorkLinkDto? link) =>
        link is null
            ? null
            : new PendingStudentWorkLinkViewModel
            {
                FileName = link.FileName,
                ViewUrl = link.ViewUrl,
                VersionNumber = link.VersionNumber,
            };

    public static EmployeeRoleCardViewModel MapRoleCard(EmployeeRoleCardDto role) => new()
    {
        RoleKey = role.RoleKey,
        RoleDisplay = role.RoleDisplay,
        PendingCount = role.PendingCount,
        Controller = role.Controller,
        Action = role.Action,
        CountsStudents = role.CountsStudents,
    };
}
