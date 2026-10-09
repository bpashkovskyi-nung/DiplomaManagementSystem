using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class StudentEnrollmentScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task StudentInSession_HasDiploma()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await IntegrationScenarioAssertions.AssertDiplomaCountInSessionAsync(
            scope.ServiceProvider,
            scenario.SessionId,
            expectedCount: 1);
    }
}
