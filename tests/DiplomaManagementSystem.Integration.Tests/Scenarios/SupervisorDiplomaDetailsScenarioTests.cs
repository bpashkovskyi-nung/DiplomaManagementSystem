using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class SupervisorDiplomaDetailsScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetDetailsAsync_AfterTopicApproved_ReturnsReadOnlyDiplomaCard()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunTopicApprovalAsync(services, scenario);

        ISupervisorDiplomaDetailsService detailsService =
            services.GetRequiredService<ISupervisorDiplomaDetailsService>();
        DiplomaDetailsDto? details = await detailsService.GetDetailsAsync(
            scenario.SupervisorId,
            scenario.DiplomaId,
            CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal("Student One", details.Header.StudentFullName);
        Assert.False(details.Actions.CanAdmit);
        Assert.False(details.Actions.ShowAdmitSection);
        Assert.False(details.Actions.CanAssignReviewer);
        Assert.False(details.Actions.ShowAssignReviewerSection);
        Assert.False(details.Actions.CanAddComment);
        Assert.False(details.Actions.ShowAddCommentSection);
        Assert.NotEmpty(details.WorkflowProgress.Steps);
        Assert.Contains(details.History.TopicVersions, version => version.Status == TopicVersionStatus.Approved);
    }

    [SkippableFact]
    public async Task GetDetailsAsync_ForAnotherSupervisor_ReturnsNull()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunTopicApprovalAsync(services, scenario);

        ISupervisorDiplomaDetailsService detailsService =
            services.GetRequiredService<ISupervisorDiplomaDetailsService>();
        DiplomaDetailsDto? details = await detailsService.GetDetailsAsync(
            scenario.ReviewerId,
            scenario.DiplomaId,
            CancellationToken.None);

        Assert.Null(details);
    }
}
