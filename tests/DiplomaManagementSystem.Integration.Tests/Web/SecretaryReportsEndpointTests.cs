using System.Net;

using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretaryReportsEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetAdmittedReport_WithSessionCookie_ReturnsAdmittedStudent()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunCheckpointsAndAdmitAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage response = await client.GetAsync("/Secretary/Reports/Admitted");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Звіт допущених");
        Assert.Contains("Student One", html, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task GetAdmittedCsv_WithSessionCookie_ReturnsCsvWithStudent()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunCheckpointsAndAdmitAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage response = await client.GetAsync("/Secretary/Reports/AdmittedCsv");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(
            "admitted-report.csv",
            response.Content.Headers.ContentDisposition?.FileName,
            StringComparison.OrdinalIgnoreCase);

        string csv = await response.Content.ReadAsStringAsync();
        Assert.Contains("Student One", csv, StringComparison.Ordinal);
        Assert.Contains(scenario.StudyGroupName, csv, StringComparison.Ordinal);
    }
}
