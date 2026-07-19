namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AreaAuthorizationEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task Unauthenticated_StudentArea_RedirectsToLogin()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync("/Student/Diploma");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Student_CannotAccessSecretaryDashboard_RedirectsToAccessDenied()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.StudentId);

        HttpResponseMessage response = await client.GetAsync("/Secretary/Dashboard");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/AccessDenied", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task Secretary_WithoutSessionCookie_RedirectsToSelectSession()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);

        HttpResponseMessage response = await client.GetAsync("/Secretary/Dashboard");

        Assert.Equal(System.Net.HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Secretary/Session", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}
