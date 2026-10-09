using System.Net;

using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class DepartmentProgressReportEndpointTests(PostgreSqlFixture fixture)
{
    // TC-INT-F8-HTTP-003
    [SkippableFact]
    public async Task GetProgress_WithSelectedSession_ReturnsOkWithReport()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(setupScope.ServiceProvider, scenario);

        ISessionSetupService sessionSetupService =
            setupScope.ServiceProvider.GetRequiredService<ISessionSetupService>();
        await sessionSetupService.SaveMilestonesAsync(
            scenario.SessionId,
            new SaveMilestonesDto(
            [
                new SaveMilestoneItemDto(new DateOnly(2026, 3, 1), 30),
                new SaveMilestoneItemDto(new DateOnly(2026, 4, 1), 60),
                new SaveMilestoneItemDto(new DateOnly(2026, 5, 1), 100),
            ]));

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.HeadId);

        HttpResponseMessage response = await client.GetAsync($"/Employee/Reports/Progress?sessionId={scenario.SessionId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.DepartmentProgressReport);
    }

    // TC-INT-F8-HTTP-004
    [SkippableFact]
    public async Task GetProgress_WithoutSessionQueryParameter_DefaultsToFirstAvailableSession()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.HeadId);

        HttpResponseMessage response = await client.GetAsync("/Employee/Reports/Progress");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.DepartmentProgressReport);
    }
}
