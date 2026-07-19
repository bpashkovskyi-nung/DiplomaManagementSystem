using System.Net;

using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class DepartmentHeadTopicEndpointTests(PostgreSqlFixture fixture)
{
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

        ISupervisorWorkflowService supervisorService = setupScope.ServiceProvider
            .GetRequiredService<ISupervisorWorkflowService>();
        await supervisorService.ApproveTopicAsync(
            scenario.SupervisorId,
            new ApproveTopicDto(versionId, null),
            CancellationToken.None);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.HeadId);

        HttpResponseMessage pageResponse = await client.GetAsync("/Employee/DepartmentHead/PendingTopics");
        pageResponse.EnsureSuccessStatusCode();
        string html = await pageResponse.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.ApproveTopicAsDepartmentHead);
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Керівник");
        Assert.Contains("data-bs-toggle=\"modal\"", html, StringComparison.Ordinal);
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Коментар (необов'язково)");
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["VersionId"] = versionId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Employee/DepartmentHead/ApproveTopic", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Employee/DepartmentHead/PendingTopics",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        IStudentDiplomaService studentService = verifyScope.ServiceProvider.GetRequiredService<IStudentDiplomaService>();
        MyDiplomaDto diploma = await studentService.GetMyDiplomaAsync(scenario.StudentId, CancellationToken.None);

        Assert.Equal(TopicVersionStatus.Approved, diploma.History.TopicVersions.Single(v => v.VersionId == versionId).Status);
    }
}
