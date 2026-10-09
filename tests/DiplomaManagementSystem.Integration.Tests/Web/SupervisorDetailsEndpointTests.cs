using System.Net;

using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SupervisorDetailsEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetDetails_AfterTopicApproved_ReturnsReadOnlyDiplomaCard()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunTopicApprovalAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SupervisorId);

        HttpResponseMessage response = await client.GetAsync($"/Employee/Supervisor/Details/{scenario.DiplomaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Student One", html, StringComparison.Ordinal);
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.MyStudentsNav);
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Робота виконується");
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.Home);
        Assert.DoesNotContain("Допустити до захисту", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
        Assert.DoesNotContain("Призначити рецензента", WebUtility.HtmlDecode(html), StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task GetDetails_ForAnotherSupervisor_ReturnsNotFound()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunTopicApprovalAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.ReviewerId);

        HttpResponseMessage response = await client.GetAsync($"/Employee/Supervisor/Details/{scenario.DiplomaId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
