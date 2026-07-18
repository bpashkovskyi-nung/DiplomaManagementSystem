using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Support;

internal static class WorkflowScenarioRunner
{
    public static async Task RunUpToTopicSubmittedAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        IStudentDiplomaService studentService = services.GetRequiredService<IStudentDiplomaService>();
        ISupervisorWorkflowService supervisorService = services.GetRequiredService<ISupervisorWorkflowService>();

        await studentService.SelectSupervisorAsync(
            scenario.StudentId,
            new SelectSupervisorDto(scenario.DiplomaId, scenario.SupervisorId),
            cancellationToken);

        await supervisorService.ConfirmStudentAsync(scenario.SupervisorId, scenario.DiplomaId, cancellationToken);

        await studentService.SubmitTopicAsync(
            scenario.StudentId,
            new SubmitTopicDto(scenario.DiplomaId, "Тема бакалаврської роботи"),
            cancellationToken);
    }

    public static async Task<Guid> GetPendingTopicVersionIdAsync(
        IServiceProvider services,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        MyDiplomaDto studentDiploma = await IntegrationScenarioAssertions.GetStudentDiplomaAsync(
            services,
            studentId,
            cancellationToken);

        return studentDiploma.History.TopicVersions
            .Single(version => version.Status == TopicVersionStatus.PendingSupervisor)
            .VersionId;
    }

    public static async Task RunTopicApprovalAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ISupervisorWorkflowService supervisorService = services.GetRequiredService<ISupervisorWorkflowService>();
        IDepartmentHeadWorkflowService headService = services.GetRequiredService<IDepartmentHeadWorkflowService>();

        await RunUpToTopicSubmittedAsync(services, scenario, cancellationToken);

        Guid versionId = await GetPendingTopicVersionIdAsync(services, scenario.StudentId, cancellationToken);

        await supervisorService.ApproveTopicAsync(
            scenario.SupervisorId,
            new ApproveTopicDto(versionId, null),
            cancellationToken);
        await headService.ApproveTopicAsync(
            scenario.HeadId,
            new ApproveTopicDto(versionId, null),
            cancellationToken);
    }

    public static async Task AssignReviewerAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        await secretaryActions.AssignReviewerAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new AssignReviewerDto(scenario.DiplomaId, scenario.ReviewerId),
            cancellationToken);
    }

    public static async Task UploadStudentWorkAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        IStudentDiplomaService studentService = services.GetRequiredService<IStudentDiplomaService>();
        await studentService.UploadWorkAsync(
            scenario.StudentId,
            scenario.DiplomaId,
            IntegrationTestDocuments.CreatePdf("student-work.pdf"),
            cancellationToken);
    }

    public static async Task RunUpToReviewerAssignedAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        await RunTopicApprovalAsync(services, scenario, cancellationToken);
        await AssignReviewerAsync(services, scenario, cancellationToken);
    }

    public static async Task RunUpToSupervisorFeedbackStepAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        await RunUpToReviewerAssignedAsync(services, scenario, cancellationToken);
        await UploadStudentWorkAsync(services, scenario, cancellationToken);

        IStudentDiplomaService studentService = services.GetRequiredService<IStudentDiplomaService>();
        await studentService.DeclareWorkReadyAsync(scenario.StudentId, scenario.DiplomaId, cancellationToken);
    }

    public static async Task RunCheckpointsAndAdmitFromSupervisorStepAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();

        await RunUpToReadyForAdmissionAsync(services, scenario, cancellationToken);

        await secretaryActions.AdmitAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new AdmitDiplomaDto(scenario.DiplomaId, new DateOnly(2026, 6, 20)),
            cancellationToken);
    }

    public static async Task RunUpToReadyForAdmissionAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        IAdmissionReviewService admissionReviewService = services.GetRequiredService<IAdmissionReviewService>();

        await admissionReviewService.CompleteSupervisorFeedbackAsync(
            scenario.SupervisorId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            IntegrationTestDocuments.CreatePdf("supervisor-feedback.pdf"),
            cancellationToken);

        await admissionReviewService.CompleteFormattingReviewAsync(
            scenario.FormattingId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            cancellationToken);

        await admissionReviewService.CompleteAntiPlagiarismAsync(
            scenario.AntiPlagiarismId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            IntegrationTestDocuments.CreatePdf("anti-plagiarism.pdf"),
            cancellationToken);

        await admissionReviewService.CompleteExternalReviewAsync(
            scenario.ReviewerId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, "OK"),
            IntegrationTestDocuments.CreatePdf("external-review.pdf"),
            cancellationToken);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(
            services,
            scenario,
            cancellationToken);
        Assert.Equal(DiplomaLifecycleStatus.ReadyForAdmission, details.State.LifecycleStatus);
    }

    public static async Task RunUpToFormattingReviewStepAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        IAdmissionReviewService admissionReviewService = services.GetRequiredService<IAdmissionReviewService>();

        await RunUpToSupervisorFeedbackStepAsync(services, scenario, cancellationToken);

        await admissionReviewService.CompleteSupervisorFeedbackAsync(
            scenario.SupervisorId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            IntegrationTestDocuments.CreatePdf("supervisor-feedback.pdf"),
            cancellationToken);
    }

    public static async Task RunUpToAntiPlagiarismStepAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        await RunUpToFormattingReviewStepAsync(services, scenario, cancellationToken);

        IAdmissionReviewService admissionReviewService = services.GetRequiredService<IAdmissionReviewService>();
        await admissionReviewService.CompleteFormattingReviewAsync(
            scenario.FormattingId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            cancellationToken);
    }

    /// <summary>
    /// Stops after topic approval so the secretary can assign a reviewer.
    /// </summary>
    public static async Task RunUpToReviewerAssignmentStepAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        await RunTopicApprovalAsync(services, scenario, cancellationToken);
    }

    public static async Task RunUpToExternalReviewStepAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        IAdmissionReviewService admissionReviewService = services.GetRequiredService<IAdmissionReviewService>();

        await RunUpToAntiPlagiarismStepAsync(services, scenario, cancellationToken);

        await admissionReviewService.CompleteAntiPlagiarismAsync(
            scenario.AntiPlagiarismId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            IntegrationTestDocuments.CreatePdf("anti-plagiarism.pdf"),
            cancellationToken);
    }

    public static async Task RunCheckpointsAndAdmitAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        await RunUpToSupervisorFeedbackStepAsync(services, scenario, cancellationToken);
        await RunCheckpointsAndAdmitFromSupervisorStepAsync(services, scenario, cancellationToken);
    }
}
