using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class TopicApprovalScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task SupervisorFlow_TopicApprovedByHead()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunTopicApprovalAsync(scope.ServiceProvider, scenario);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(
            scope.ServiceProvider,
            scenario);
        IntegrationScenarioAssertions.AssertTopicApproved(details);
    }
}
