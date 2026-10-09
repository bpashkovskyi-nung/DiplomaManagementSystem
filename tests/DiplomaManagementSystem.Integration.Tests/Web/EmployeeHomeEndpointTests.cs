using System.Net;

using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class EmployeeHomeEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetHome_AfterTopicSubmitted_ShowsExpectedTileTitles()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SupervisorId);

        HttpResponseMessage response = await client.GetAsync("/Employee/Home/Index");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.Home);
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.SupervisorRole);
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.MyStudentsNav);
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.ApproveTopicNav);
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.SubmitSupervisorFeedbackNav);
        Assert.Contains("/Employee/Supervisor/Students", html, StringComparison.Ordinal);
    }
}
