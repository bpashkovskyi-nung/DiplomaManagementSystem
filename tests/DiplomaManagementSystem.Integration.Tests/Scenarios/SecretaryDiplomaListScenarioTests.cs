using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretaryDiplomaListScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetList_FilterByLifecycle_ReturnsMatchingDiploma()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunTopicApprovalAsync(scope.ServiceProvider, scenario);

        ISecretaryDiplomaListService listService = scope.ServiceProvider.GetRequiredService<ISecretaryDiplomaListService>();
        DiplomaListPageDto? page = await listService.GetListAsync(
            scenario.SessionId,
            new DiplomaListFilterDto(
                LifecycleStatus: DiplomaLifecycleStatus.TopicApproved,
                CurrentAdmissionStep: null,
                SupervisorAssignmentStatus: null,
                AdmissionStatus: null,
                StudyGroupId: null,
                Search: null),
            CancellationToken.None);

        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.Id == scenario.DiplomaId);
    }

    [SkippableFact]
    public async Task GetList_SearchByStudentName_ReturnsMatchingDiploma()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();

        ISecretaryDiplomaListService listService = scope.ServiceProvider.GetRequiredService<ISecretaryDiplomaListService>();
        DiplomaListPageDto? page = await listService.GetListAsync(
            scenario.SessionId,
            new DiplomaListFilterDto(
                LifecycleStatus: null,
                CurrentAdmissionStep: null,
                SupervisorAssignmentStatus: null,
                AdmissionStatus: null,
                StudyGroupId: null,
                Search: "Student One"),
            CancellationToken.None);

        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.Id == scenario.DiplomaId);
    }

    [SkippableFact]
    public async Task GetList_ComboFilterLifecycleAndAdmissionStep_ReturnsMatchingDiploma()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(scope.ServiceProvider, scenario);

        ISecretaryDiplomaListService listService = scope.ServiceProvider.GetRequiredService<ISecretaryDiplomaListService>();
        DiplomaListPageDto? page = await listService.GetListAsync(
            scenario.SessionId,
            new DiplomaListFilterDto(
                LifecycleStatus: DiplomaLifecycleStatus.DocumentsInProgress,
                CurrentAdmissionStep: AdmissionStep.SupervisorFeedback,
                SupervisorAssignmentStatus: null,
                AdmissionStatus: null,
                StudyGroupId: null,
                Search: null),
            CancellationToken.None);

        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.Id == scenario.DiplomaId);
    }

    [SkippableFact]
    public async Task GetList_WhenSessionNotFound_ReturnsNull()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        ISecretaryDiplomaListService listService = scope.ServiceProvider.GetRequiredService<ISecretaryDiplomaListService>();

        DiplomaListPageDto? page = await listService.GetListAsync(
            Guid.NewGuid(),
            new DiplomaListFilterDto(null, null, null, null, null, null),
            CancellationToken.None);

        Assert.Null(page);
    }

    [SkippableFact]
    public async Task GetList_FilterByStudyGroup_ReturnsMatchingDiploma()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        ISecretaryDiplomaListService listService = scope.ServiceProvider.GetRequiredService<ISecretaryDiplomaListService>();
        DiplomaListPageDto? page = await listService.GetListAsync(
            scenario.SessionId,
            new DiplomaListFilterDto(
                LifecycleStatus: null,
                CurrentAdmissionStep: null,
                SupervisorAssignmentStatus: null,
                AdmissionStatus: null,
                StudyGroupId: scenario.GroupId,
                Search: null),
            CancellationToken.None);

        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.Id == scenario.DiplomaId);
        Assert.DoesNotContain(page.Items, item => item.StudyGroupName != scenario.StudyGroupName);
    }

    [SkippableFact]
    public async Task GetList_FilterByAdmissionStatus_ReturnsNotAdmittedOnly()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        ISecretaryDiplomaListService listService = scope.ServiceProvider.GetRequiredService<ISecretaryDiplomaListService>();
        DiplomaListPageDto? page = await listService.GetListAsync(
            scenario.SessionId,
            new DiplomaListFilterDto(
                LifecycleStatus: null,
                CurrentAdmissionStep: null,
                SupervisorAssignmentStatus: null,
                AdmissionStatus: DiplomaAdmissionStatus.NotAdmitted,
                StudyGroupId: null,
                Search: null),
            CancellationToken.None);

        Assert.NotNull(page);
        Assert.Contains(page.Items, item => item.Id == scenario.DiplomaId);
        Assert.All(page.Items, item => Assert.Equal(DiplomaAdmissionStatus.NotAdmitted, item.AdmissionStatus));
    }
}
