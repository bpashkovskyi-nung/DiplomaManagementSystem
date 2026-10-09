namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class HealthEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task Health_ReturnsOk()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/healthchecks");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
    }
}
