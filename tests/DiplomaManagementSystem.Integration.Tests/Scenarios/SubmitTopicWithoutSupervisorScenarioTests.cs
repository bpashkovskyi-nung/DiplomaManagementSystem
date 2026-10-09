using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class SubmitTopicWithoutSupervisorScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task SubmitTopic_BeforeSupervisorConfirmed_ThrowsDomainException()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IStudentDiplomaService studentService = scope.ServiceProvider.GetRequiredService<IStudentDiplomaService>();

        await studentService.SelectSupervisorAsync(
            scenario.StudentId,
            new SelectSupervisorDto(scenario.DiplomaId, scenario.SupervisorId),
            CancellationToken.None);

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            studentService.SubmitTopicAsync(
                scenario.StudentId,
                new SubmitTopicDto(scenario.DiplomaId, "Тема без підтвердження"),
                CancellationToken.None));

        Assert.Contains("confirmed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
