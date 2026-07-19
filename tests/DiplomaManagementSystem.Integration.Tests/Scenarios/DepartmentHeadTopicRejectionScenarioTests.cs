using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class DepartmentHeadTopicRejectionScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task HeadRejectsTopic_TopicMarkedRejected()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);
        Guid versionId = await WorkflowScenarioRunner.GetPendingTopicVersionIdAsync(
            services,
            scenario.StudentId);

        ISupervisorWorkflowService supervisorService = services.GetRequiredService<ISupervisorWorkflowService>();
        await supervisorService.ApproveTopicAsync(
            scenario.SupervisorId,
            new ApproveTopicDto(versionId, null),
            CancellationToken.None);

        IDepartmentHeadWorkflowService headService = services.GetRequiredService<IDepartmentHeadWorkflowService>();
        await headService.RejectTopicAsync(
            scenario.HeadId,
            new ReviewTopicDto(versionId, "Тема потребує перегляду"),
            CancellationToken.None);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        IntegrationScenarioAssertions.AssertTopicRejected(details);
    }
}
