using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretaryDashboardScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetDashboard_ReturnsBucketsForSession()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        ISecretaryDashboardService dashboardService = scope.ServiceProvider.GetRequiredService<ISecretaryDashboardService>();

        SecretaryDashboardDto? dashboard = await dashboardService.GetDashboardAsync(
            scenario.SessionId,
            CancellationToken.None);

        Assert.NotNull(dashboard);
        Assert.Equal(scenario.SessionId, dashboard.SessionId);
        Assert.True(dashboard.TotalDiplomas >= 1);
        Assert.NotEmpty(dashboard.Buckets);
    }
}
