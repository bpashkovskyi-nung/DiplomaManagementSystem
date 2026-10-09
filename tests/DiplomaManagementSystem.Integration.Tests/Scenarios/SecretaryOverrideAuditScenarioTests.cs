using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretaryOverrideAuditScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task OverrideSupervisor_WritesAuditLog()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);

        IUserProvisioningService userProvisioningService = services.GetRequiredService<IUserProvisioningService>();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        ApplicationUser replacementSupervisor = await userProvisioningService.CreateEmployeeAsync(
            "Supervisor Two",
            $"supervisor2.{suffix}@test.local");
        await IntegrationDepartmentHelper.AssignEmployeeAsync(
            services,
            replacementSupervisor.Id,
            replacementSupervisor.FullName);

        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        await secretaryActions.OverrideSupervisorAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new OverrideSupervisorDto(scenario.DiplomaId, replacementSupervisor.Id, "Заміна за рішенням комісії"),
            CancellationToken.None);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        Assert.Equal(replacementSupervisor.Id, details.Assignments.SupervisorId);

        await IntegrationScenarioAssertions.AssertAuditLogExistsAsync(
            services,
            scenario.DiplomaId,
            "OverrideSupervisor",
            scenario.SecretaryId);
    }
}
