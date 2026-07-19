using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Employee;

internal sealed class EmployeeHomeService(
    IEmployeeHomeQueries employeeHomeQueries,
    IAnnualRoleQueries annualRoleQueries,
    ISupervisorWorkflowService supervisorWorkflowService,
    ISupervisorDiplomaListService supervisorDiplomaListService,
    IReviewerDiplomaListService reviewerDiplomaListService,
    IDepartmentHeadWorkflowService departmentHeadWorkflowService,
    IAdmissionReviewService admissionReviewService,
    CurrentDepartmentResolver currentDepartmentResolver) : IEmployeeHomeService
{
    public async Task<EmployeeHomeDto> GetHomeAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        Guid departmentId = await currentDepartmentResolver.ResolveRequiredEmployeeDepartmentIdAsync(
            employeeId,
            cancellationToken);

        List<EmployeeRoleCardDto> roles = [];

        int pendingStudents = (await supervisorWorkflowService.GetPendingStudentsAsync(employeeId, cancellationToken)).Count;
        int pendingSupervisorTopics = (await supervisorWorkflowService.GetTopicReviewsAsync(employeeId, cancellationToken)).Count;
        bool isSupervisor = pendingStudents > 0
                            || pendingSupervisorTopics > 0
                            || await employeeHomeQueries.HasAnySupervisorDiplomasAsync(
                                employeeId,
                                departmentId,
                                cancellationToken);

        int pendingReviewerAssignments = (await admissionReviewService.GetReviewerAssignmentsAsync(
            employeeId,
            cancellationToken)).Count;

        bool isReviewer = pendingReviewerAssignments > 0
                          || await employeeHomeQueries.HasAnyReviewerDiplomasAsync(
                              employeeId,
                              departmentId,
                              cancellationToken);

        if (isSupervisor)
        {
            SupervisorDiplomaListPageDto studentsPage = await supervisorDiplomaListService.GetListAsync(
                employeeId,
                new DiplomaListFilterDto(null, null, null, null, null, null),
                cancellationToken);

            roles.Add(new EmployeeRoleCardDto(
                "SupervisorStudents",
                EmployeePageTitles.MyStudents,
                studentsPage.Items.Count,
                "Supervisor",
                "Students",
                CountsStudents: true));
        }

        if (isReviewer)
        {
            ReviewerDiplomaListPageDto reviewStudentsPage = await reviewerDiplomaListService.GetListAsync(
                employeeId,
                new DiplomaListFilterDto(null, null, null, null, null, null),
                cancellationToken);

            roles.Add(new EmployeeRoleCardDto(
                "ReviewerStudents",
                EmployeePageTitles.MyReviewStudents,
                reviewStudentsPage.Items.Count,
                "Reviewer",
                "Students",
                CountsStudents: true));
        }

        if (isSupervisor)
        {
            roles.Add(new EmployeeRoleCardDto(
                "SupervisorPendingStudents",
                EmployeePageTitles.ConfirmStudentRequest,
                pendingStudents,
                "Supervisor",
                "PendingStudents"));

            roles.Add(new EmployeeRoleCardDto(
                "SupervisorTopicReviews",
                EmployeePageTitles.ApproveTopicAsSupervisor,
                pendingSupervisorTopics,
                "Supervisor",
                "TopicReviews"));
        }

        List<Guid> departmentHeadSessionIds = await annualRoleQueries.GetSessionIdsAsync(
            employeeId,
            AnnualRoleType.DepartmentHead,
            cancellationToken);

        if (departmentHeadSessionIds.Count > 0)
        {
            int pendingHeadTopics = (await departmentHeadWorkflowService.GetPendingTopicsAsync(employeeId, cancellationToken)).Count;

            roles.Add(new EmployeeRoleCardDto(
                "DepartmentHead",
                EmployeePageTitles.ApproveTopicAsDepartmentHead,
                pendingHeadTopics,
                "DepartmentHead",
                "PendingTopics"));
        }

        int pendingSupervisorFeedback = (await admissionReviewService.GetSupervisorFeedbackPendingAsync(
            employeeId,
            cancellationToken)).Count;

        if (isSupervisor)
        {
            roles.Add(new EmployeeRoleCardDto(
                "SupervisorFeedback",
                EmployeePageTitles.SubmitSupervisorFeedback,
                pendingSupervisorFeedback,
                "Supervisor",
                "Checkpoints"));
        }

        if (isReviewer)
        {
            roles.Add(new EmployeeRoleCardDto(
                "Reviewer",
                EmployeePageTitles.SubmitExternalReview,
                pendingReviewerAssignments,
                "Reviewer",
                "Assignments"));
        }

        List<Guid> antiPlagiarismSessionIds = await annualRoleQueries.GetSessionIdsAsync(
            employeeId,
            AnnualRoleType.AntiPlagiarismOfficer,
            cancellationToken);

        if (antiPlagiarismSessionIds.Count > 0)
        {
            int pendingAntiPlagiarism = (await admissionReviewService.GetAntiPlagiarismPendingAsync(
                employeeId,
                cancellationToken)).Count;

            roles.Add(new EmployeeRoleCardDto(
                "AntiPlagiarism",
                EmployeePageTitles.AntiPlagiarism,
                pendingAntiPlagiarism,
                "AntiPlagiarism",
                "Pending"));
        }

        List<Guid> formattingSessionIds = await annualRoleQueries.GetSessionIdsAsync(
            employeeId,
            AnnualRoleType.FormattingReviewer,
            cancellationToken);

        if (formattingSessionIds.Count > 0)
        {
            int pendingFormatting = (await admissionReviewService.GetFormattingReviewPendingAsync(
                employeeId,
                cancellationToken)).Count;

            roles.Add(new EmployeeRoleCardDto(
                "FormattingReview",
                EmployeePageTitles.FormattingReview,
                pendingFormatting,
                "FormattingReview",
                "Pending"));
        }

        return new EmployeeHomeDto(roles);
    }
}
