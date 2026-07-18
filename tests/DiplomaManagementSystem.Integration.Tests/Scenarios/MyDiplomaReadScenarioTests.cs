using DiplomaManagementSystem.Application.Student;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class MyDiplomaReadScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetMyDiploma_AfterTopicApproval_ReturnsCompositeDto()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunTopicApprovalAsync(services, scenario);

        MyDiplomaDto dto = await IntegrationScenarioAssertions.GetStudentDiplomaAsync(
            services,
            scenario.StudentId);

        IntegrationScenarioAssertions.AssertMyDiplomaCompositeShape(dto);
        Assert.Equal(scenario.DiplomaId, dto.Header.DiplomaId);
        Assert.Equal(TopicVersionStatus.Approved, dto.Assignments.TopicStatus);
        Assert.Equal(DiplomaLifecycleStatus.TopicApproved, dto.State!.LifecycleStatus);

        StudentWorkflowStepDto approvedTopicStep = dto.WorkflowProgress!.Steps[1];
        Assert.Equal(StudentWorkflowStepState.Completed, approvedTopicStep.State);
        Assert.Equal("Тема на розгляді", approvedTopicStep.Title);
        Assert.NotNull(approvedTopicStep.Detail);
        Assert.Contains("Supervisor One", approvedTopicStep.Detail, StringComparison.Ordinal);
        Assert.Contains("Head One", approvedTopicStep.Detail, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task GetMyDiploma_WithoutDiploma_ReturnsEmptyComposite()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        StudentOnlyScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedStudentWithoutDiplomaAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        MyDiplomaDto dto = await IntegrationScenarioAssertions.GetStudentDiplomaAsync(
            scope.ServiceProvider,
            scenario.StudentId);

        IntegrationScenarioAssertions.AssertEmptyMyDiploma(dto);
    }
}
