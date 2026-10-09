using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class TopicRejectionScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task SupervisorRejectsTopic_StudentCannotDeclareWorkReady()
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
        await supervisorService.RejectTopicAsync(
            scenario.SupervisorId,
            new ReviewTopicDto(versionId, "Потрібно уточнити формулювання"),
            CancellationToken.None);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        IntegrationScenarioAssertions.AssertTopicRejectedBySupervisor(details);

        IStudentDiplomaService studentService = services.GetRequiredService<IStudentDiplomaService>();
        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            studentService.DeclareWorkReadyAsync(scenario.StudentId, scenario.DiplomaId, CancellationToken.None));

        Assert.Contains("work file", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
