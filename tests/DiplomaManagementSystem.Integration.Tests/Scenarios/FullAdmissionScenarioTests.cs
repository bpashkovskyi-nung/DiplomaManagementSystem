using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class FullAdmissionScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task FullFlow_CheckpointsToAdmit()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using (AsyncServiceScope prepScope = fixture.CreateProvider().CreateAsyncScope())
        {
            await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(
                prepScope.ServiceProvider,
                scenario);
        }

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunCheckpointsAndAdmitFromSupervisorStepAsync(
            scope.ServiceProvider,
            scenario);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(
            scope.ServiceProvider,
            scenario);
        IntegrationScenarioAssertions.AssertAdmitted(details);
    }
}
