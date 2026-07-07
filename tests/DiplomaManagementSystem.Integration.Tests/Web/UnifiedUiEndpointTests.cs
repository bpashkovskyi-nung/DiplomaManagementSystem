using System.Net;
using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class UnifiedUiEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetSupervisorStudents_ShowsSupervisorWorkflowNavigation()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SupervisorId);

        HttpResponseMessage response = await client.GetAsync("/Employee/Supervisor/Students");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        UnifiedUiHtmlAssertions.AssertContainsNavTitles(
            html,
            EmployeePageTitles.MyStudents,
            EmployeePageTitles.ConfirmStudentRequest,
            EmployeePageTitles.ApproveTopicAsSupervisor,
            EmployeePageTitles.SubmitSupervisorFeedback,
            EmployeePageTitles.Home);
        Assert.Contains("table table-striped align-middle", html, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task GetSupervisorCheckpoints_ShowsUnifiedCheckpointQueue()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SupervisorId);

        HttpResponseMessage response = await client.GetAsync("/Employee/Supervisor/Checkpoints");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.SubmitSupervisorFeedback);
        UnifiedUiHtmlAssertions.AssertCheckpointQueueTable(html);
        UnifiedUiHtmlAssertions.AssertContainsNavTitles(
            html,
            EmployeePageTitles.ConfirmStudentRequest,
            EmployeePageTitles.ApproveTopicAsSupervisor);
    }

    [SkippableFact]
    public async Task GetReviewerAssignments_ShowsStudyGroupInTable()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToExternalReviewStepAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.ReviewerId);

        HttpResponseMessage response = await client.GetAsync("/Employee/Reviewer/Assignments");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.SubmitExternalReview);
        UnifiedUiHtmlAssertions.AssertCheckpointQueueTable(html);
        IntegrationTestHtmlAssertions.AssertContainsText(html, scenario.StudyGroupName);
        Assert.Contains("Student One", html, StringComparison.Ordinal);
        UnifiedUiHtmlAssertions.AssertContainsNavTitles(
            html,
            EmployeePageTitles.MyReviewStudents,
            EmployeePageTitles.Home);
    }

    [SkippableFact]
    public async Task GetAntiPlagiarismPending_ShowsUnifiedCheckpointQueue()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToAntiPlagiarismStepAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.AntiPlagiarismId);

        HttpResponseMessage response = await client.GetAsync("/Employee/AntiPlagiarism/Pending");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.AntiPlagiarism);
        UnifiedUiHtmlAssertions.AssertCheckpointQueueTable(html);
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.Home);
    }

    [SkippableFact]
    public async Task GetFormattingPending_ShowsUnifiedCheckpointQueueWithoutFileField()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(setupScope.ServiceProvider, scenario);

        IAdmissionReviewService admissionReviewService =
            setupScope.ServiceProvider.GetRequiredService<IAdmissionReviewService>();
        await admissionReviewService.CompleteSupervisorFeedbackAsync(
            scenario.SupervisorId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            IntegrationTestDocuments.CreatePdf("supervisor-feedback.pdf"),
            CancellationToken.None);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.FormattingId);

        HttpResponseMessage response = await client.GetAsync("/Employee/FormattingReview/Pending");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.FormattingReview);
        UnifiedUiHtmlAssertions.AssertCheckpointQueueTable(html);
        Assert.DoesNotContain("name=\"Document\"", html, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task GetDepartmentHeadPendingTopics_ShowsHomeNavigation()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(setupScope.ServiceProvider, scenario);

        ISupervisorWorkflowService supervisorService =
            setupScope.ServiceProvider.GetRequiredService<ISupervisorWorkflowService>();
        Guid versionId = await WorkflowScenarioRunner.GetPendingTopicVersionIdAsync(
            setupScope.ServiceProvider,
            scenario.StudentId);
        await supervisorService.ApproveTopicAsync(
            scenario.SupervisorId,
            new ApproveTopicDto(versionId, null),
            CancellationToken.None);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.HeadId);

        HttpResponseMessage response = await client.GetAsync("/Employee/DepartmentHead/PendingTopics");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.ApproveTopicAsDepartmentHead);
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.Home);
        Assert.Contains("table table-striped align-middle", html, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task GetSecretaryDiplomas_ShowsDocumentsNavigation()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage response = await client.GetAsync("/Secretary/Diplomas");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        UnifiedUiHtmlAssertions.AssertContainsNavTitles(
            html,
            SecretaryPageTitles.Home,
            SecretaryPageTitles.TopicOrder,
            SecretaryPageTitles.AdmittedReport);
        Assert.Contains("table table-striped align-middle", html, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task GetSecretaryTopicOrder_ShowsDocumentsNavigation()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage response = await client.GetAsync("/Secretary/Documents/TopicOrder");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        IntegrationTestHtmlAssertions.AssertContainsText(html, SecretaryPageTitles.TopicOrder);
        UnifiedUiHtmlAssertions.AssertContainsNavTitles(
            html,
            SecretaryPageTitles.Home,
            SecretaryPageTitles.AdmittedReport);
    }

    [SkippableFact]
    public async Task GetAdminDefenceSessions_ShowsGlobalNavigation()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync("/Admin/DefenceSessions");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        IntegrationTestHtmlAssertions.AssertContainsText(html, AdminPageTitles.DefenceSessions);
        UnifiedUiHtmlAssertions.AssertContainsNavTitles(
            html,
            AdminPageTitles.Home,
            AdminPageTitles.Employees);
        Assert.Contains("table table-striped align-middle", html, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task GetAdminEmployees_ShowsGlobalNavigation()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync("/Admin/Employees");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        IntegrationTestHtmlAssertions.AssertContainsText(html, AdminPageTitles.Employees);
        UnifiedUiHtmlAssertions.AssertContainsNavTitles(
            html,
            AdminPageTitles.Home,
            AdminPageTitles.DefenceSessions);
        Assert.Contains("table table-striped align-middle", html, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task GetAdminEmployeeWorkloadLimits_ShowsSessionNavigation()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        Guid sessionId = await new IntegrationScenarioBuilder(fixture.CreateProvider()).SeedSessionOnlyAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync($"/Admin/EmployeeWorkloadLimits?defenceSessionId={sessionId}");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        IntegrationTestHtmlAssertions.AssertContainsText(html, AdminPageTitles.EmployeeWorkloadLimits);
        UnifiedUiHtmlAssertions.AssertContainsNavTitles(
            html,
            AdminPageTitles.DefenceSessions,
            AdminPageTitles.DefenceSession,
            AdminPageTitles.Students,
            AdminPageTitles.Home);
    }
}
