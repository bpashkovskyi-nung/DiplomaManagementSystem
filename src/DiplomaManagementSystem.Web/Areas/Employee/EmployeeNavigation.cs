using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Web.Areas.Employee.Models;

namespace DiplomaManagementSystem.Web.Areas.Employee;

internal static class EmployeeNavigation
{
    public static IReadOnlyList<EmployeeNavLink> SupervisorWorkflow() =>
    [
        new(EmployeePageTitles.MyStudents, "Supervisor", "Students"),
        new(EmployeePageTitles.ConfirmStudentRequest, "Supervisor", "PendingStudents"),
        new(EmployeePageTitles.ApproveTopicAsSupervisor, "Supervisor", "TopicReviews"),
        new(EmployeePageTitles.SubmitSupervisorFeedback, "Supervisor", "Checkpoints"),
        new(EmployeePageTitles.Home, "Home", "Index"),
    ];

    public static IReadOnlyList<EmployeeNavLink> ReviewerWorkflow() =>
    [
        new(EmployeePageTitles.MyReviewStudents, "Reviewer", "Students"),
        new(EmployeePageTitles.SubmitExternalReview, "Reviewer", "Assignments"),
        new(EmployeePageTitles.Home, "Home", "Index"),
    ];

    public static IReadOnlyList<EmployeeNavLink> SupervisorDetailsBack() =>
    [
        new(EmployeePageTitles.MyStudents, "Supervisor", "Students"),
        new(EmployeePageTitles.Home, "Home", "Index"),
    ];

    public static IReadOnlyList<EmployeeNavLink> HomeOnly() =>
    [
        new(EmployeePageTitles.Home, "Home", "Index"),
    ];
}
