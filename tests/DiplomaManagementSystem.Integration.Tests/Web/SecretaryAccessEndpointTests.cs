using System.Net;

using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretaryAccessEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetDashboard_WithInaccessibleSessionCookie_RedirectsToSelectSession()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(setupScope.ServiceProvider);
        IDefenceSessionService defenceSessionService = setupScope.ServiceProvider.GetRequiredService<IDefenceSessionService>();
        Guid otherSessionId = await defenceSessionService.CreateAsync(
            new DefenceSessionFormDto(null, 2025, DefenceSessionType.Master, 1));

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, otherSessionId);

        HttpResponseMessage response = await client.GetAsync("/Secretary/Dashboard");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            "/Secretary/Session",
            response.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }
}
