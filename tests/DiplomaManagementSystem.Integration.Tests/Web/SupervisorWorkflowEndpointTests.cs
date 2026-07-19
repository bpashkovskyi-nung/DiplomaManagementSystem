using System.Net;

using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SupervisorWorkflowEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostConfirm_RedirectsAndConfirmsStudent()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient studentClient = IntegrationTestWebClient.CreateClient(factory, scenario.StudentId);

        HttpResponseMessage studentPage = await studentClient.GetAsync("/Student/Diploma");
        studentPage.EnsureSuccessStatusCode();
        string studentHtml = await studentPage.Content.ReadAsStringAsync();
        string studentToken = AntiforgeryTokenParser.Parse(studentHtml);

        FormUrlEncodedContent selectForm = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = studentToken,
            ["DiplomaId"] = scenario.DiplomaId.ToString(),
            ["SupervisorId"] = scenario.SupervisorId.ToString(),
        });
        await studentClient.PostAsync("/Student/Diploma/SelectSupervisor", selectForm);

        HttpClient supervisorClient = IntegrationTestWebClient.CreateClient(factory, scenario.SupervisorId);
        HttpResponseMessage pendingPage = await supervisorClient.GetAsync("/Employee/Supervisor/PendingStudents");
        pendingPage.EnsureSuccessStatusCode();
        string html = await pendingPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["diplomaId"] = scenario.DiplomaId.ToString(),
        });

        HttpResponseMessage postResponse = await supervisorClient.PostAsync("/Employee/Supervisor/Confirm", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Employee/Supervisor/PendingStudents",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        IStudentDiplomaService studentService = verifyScope.ServiceProvider.GetRequiredService<IStudentDiplomaService>();
        MyDiplomaDto diploma = await studentService.GetMyDiplomaAsync(scenario.StudentId, CancellationToken.None);

        Assert.Equal(SupervisorAssignmentStatus.Confirmed, diploma.Assignments.SupervisorAssignmentStatus);
    }

    [SkippableFact]
    public async Task PostApproveTopic_RedirectsAndApprovesTopic()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(setupScope.ServiceProvider, scenario);
        Guid versionId = await WorkflowScenarioRunner.GetPendingTopicVersionIdAsync(
            setupScope.ServiceProvider,
            scenario.StudentId);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SupervisorId);

        HttpResponseMessage pageResponse = await client.GetAsync("/Employee/Supervisor/TopicReviews");
        pageResponse.EnsureSuccessStatusCode();
        string html = await pageResponse.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["VersionId"] = versionId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Employee/Supervisor/ApproveTopic", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Employee/Supervisor/TopicReviews",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        IStudentDiplomaService studentService = verifyScope.ServiceProvider.GetRequiredService<IStudentDiplomaService>();
        MyDiplomaDto diploma = await studentService.GetMyDiplomaAsync(scenario.StudentId, CancellationToken.None);

        Assert.Equal(TopicVersionStatus.PendingHead, diploma.History.TopicVersions.Single(v => v.VersionId == versionId).Status);
    }
}
