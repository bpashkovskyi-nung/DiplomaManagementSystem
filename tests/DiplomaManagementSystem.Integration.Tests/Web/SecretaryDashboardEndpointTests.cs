using System.Net;

using DiplomaManagementSystem.Integration.Tests.Support;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretaryDashboardEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetDashboard_WithSessionCookie_ReturnsSuccess()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage response = await client.GetAsync("/Secretary/Dashboard");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Кабінет секретаря");
        Assert.Contains("2026", html, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task GetDiplomaDetails_WithSessionCookie_ReturnsSuccess()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage response = await client.GetAsync($"/Secretary/Diplomas/Details/{scenario.DiplomaId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Student One", html, StringComparison.Ordinal);
    }
}
