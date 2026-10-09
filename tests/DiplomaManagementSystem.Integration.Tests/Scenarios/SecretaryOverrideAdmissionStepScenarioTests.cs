using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretaryOverrideAdmissionStepScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task OverrideAdmissionStep_WritesAuditAndAdvancesLifecycle()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(services, scenario);

        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        await secretaryActions.OverrideAdmissionStepAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new OverrideAdmissionStepDto(
                scenario.DiplomaId,
                AdmissionStep.SupervisorFeedback,
                CheckpointOutcome.Approved,
                "Примусове погодження відгуку"),
            CancellationToken.None);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        Assert.Equal(AdmissionStep.FormattingReview, details.State.CurrentAdmissionStep);
        Assert.Contains(
            details.History.AttemptHistory,
            attempt => attempt.Step == AdmissionStep.SupervisorFeedback && attempt.IsSecretaryOverride);

        await IntegrationScenarioAssertions.AssertAuditLogExistsByActionAsync(
            services,
            "SecretaryOverrideAdmissionStep",
            scenario.SecretaryId);
    }
}
