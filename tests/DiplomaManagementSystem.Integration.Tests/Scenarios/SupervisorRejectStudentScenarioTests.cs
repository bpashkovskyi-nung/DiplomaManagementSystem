using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class SupervisorRejectStudentScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task SupervisorRejectsStudent_StudentCanSelectAnotherSupervisor()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        IStudentDiplomaService studentService = services.GetRequiredService<IStudentDiplomaService>();
        await studentService.SelectSupervisorAsync(
            scenario.StudentId,
            new SelectSupervisorDto(scenario.DiplomaId, scenario.SupervisorId),
            CancellationToken.None);

        ISupervisorWorkflowService supervisorService = services.GetRequiredService<ISupervisorWorkflowService>();
        await supervisorService.RejectStudentAsync(
            scenario.SupervisorId,
            new SupervisorActionDto(scenario.DiplomaId, "Не можу керувати цією роботою"),
            CancellationToken.None);

        MyDiplomaDto diploma = await IntegrationScenarioAssertions.GetStudentDiplomaAsync(
            services,
            scenario.StudentId);
        Assert.Equal(SupervisorAssignmentStatus.Rejected, diploma.Assignments.SupervisorAssignmentStatus);
        Assert.True(diploma.Actions!.CanSelectSupervisor);
    }
}
