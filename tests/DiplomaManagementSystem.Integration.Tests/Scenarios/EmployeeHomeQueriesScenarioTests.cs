using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class EmployeeHomeQueriesScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task CountPendingSupervisorStudentsAsync_WhenStudentSelected_ReturnsOne()
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

        IEmployeeHomeQueries queries = services.GetRequiredService<IEmployeeHomeQueries>();
        int count = await queries.CountPendingSupervisorStudentsAsync(scenario.SupervisorId, CancellationToken.None);

        Assert.Equal(1, count);
    }

    [SkippableFact]
    public async Task CountPendingSupervisorTopicsAsync_WhenTopicSubmitted_ReturnsOne()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(scope.ServiceProvider, scenario);

        IEmployeeHomeQueries queries = scope.ServiceProvider.GetRequiredService<IEmployeeHomeQueries>();
        int count = await queries.CountPendingSupervisorTopicsAsync(scenario.SupervisorId, CancellationToken.None);

        Assert.Equal(1, count);
    }

    [SkippableFact]
    public async Task HasAnySupervisorDiplomasAsync_AfterTopicApproval_ReturnsTrue()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunTopicApprovalAsync(scope.ServiceProvider, scenario);

        IEmployeeHomeQueries queries = scope.ServiceProvider.GetRequiredService<IEmployeeHomeQueries>();
        bool hasDiplomas = await queries.HasAnySupervisorDiplomasAsync(scenario.SupervisorId, null, CancellationToken.None);

        Assert.True(hasDiplomas);
    }

    [SkippableFact]
    public async Task CountPendingHeadTopicsAsync_WhenSupervisorApproved_ReturnsOne()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);
        Guid versionId = await WorkflowScenarioRunner.GetPendingTopicVersionIdAsync(services, scenario.StudentId);
        ISupervisorWorkflowService supervisorService = services.GetRequiredService<ISupervisorWorkflowService>();
        await supervisorService.ApproveTopicAsync(
            scenario.SupervisorId,
            new ApproveTopicDto(versionId, null),
            CancellationToken.None);

        IEmployeeHomeQueries queries = services.GetRequiredService<IEmployeeHomeQueries>();
        int count = await queries.CountPendingHeadTopicsAsync([scenario.SessionId], CancellationToken.None);

        Assert.Equal(1, count);
    }

    [SkippableFact]
    public async Task CountPendingHeadTopicsAsync_WhenSessionIdsEmpty_ReturnsZero()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        await new IntegrationScenarioBuilder(fixture.CreateProvider()).SeedSessionOnlyAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IEmployeeHomeQueries queries = scope.ServiceProvider.GetRequiredService<IEmployeeHomeQueries>();

        int count = await queries.CountPendingHeadTopicsAsync([], CancellationToken.None);

        Assert.Equal(0, count);
    }

    [SkippableFact]
    public async Task CountPendingSupervisorFeedbackAsync_WhenWorkReady_ReturnsOne()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(scope.ServiceProvider, scenario);

        IEmployeeHomeQueries queries = scope.ServiceProvider.GetRequiredService<IEmployeeHomeQueries>();
        int count = await queries.CountPendingSupervisorFeedbackAsync(scenario.SupervisorId, CancellationToken.None);

        Assert.Equal(1, count);
    }

    [SkippableFact]
    public async Task CountPendingFormattingReviewAsync_WhenSupervisorFeedbackComplete_ReturnsOne()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToFormattingReviewStepAsync(scope.ServiceProvider, scenario);

        IEmployeeHomeQueries queries = scope.ServiceProvider.GetRequiredService<IEmployeeHomeQueries>();
        int count = await queries.CountPendingFormattingReviewAsync([scenario.SessionId], CancellationToken.None);

        Assert.Equal(1, count);
    }

    [SkippableFact]
    public async Task CountPendingFormattingReviewAsync_WhenSessionIdsEmpty_ReturnsZero()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        await new IntegrationScenarioBuilder(fixture.CreateProvider()).SeedSessionOnlyAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IEmployeeHomeQueries queries = scope.ServiceProvider.GetRequiredService<IEmployeeHomeQueries>();

        int count = await queries.CountPendingFormattingReviewAsync([], CancellationToken.None);

        Assert.Equal(0, count);
    }

    [SkippableFact]
    public async Task CountPendingAntiPlagiarismAsync_WhenFormattingComplete_ReturnsOne()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToAntiPlagiarismStepAsync(scope.ServiceProvider, scenario);

        IEmployeeHomeQueries queries = scope.ServiceProvider.GetRequiredService<IEmployeeHomeQueries>();
        int count = await queries.CountPendingAntiPlagiarismAsync([scenario.SessionId], CancellationToken.None);

        Assert.Equal(1, count);
    }

    [SkippableFact]
    public async Task CountPendingAntiPlagiarismAsync_WhenSessionIdsEmpty_ReturnsZero()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        await new IntegrationScenarioBuilder(fixture.CreateProvider()).SeedSessionOnlyAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IEmployeeHomeQueries queries = scope.ServiceProvider.GetRequiredService<IEmployeeHomeQueries>();

        int count = await queries.CountPendingAntiPlagiarismAsync([], CancellationToken.None);

        Assert.Equal(0, count);
    }

    [SkippableFact]
    public async Task CountPendingReviewerAssignmentsAsync_WhenReviewerAssigned_ReturnsOne()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToExternalReviewStepAsync(scope.ServiceProvider, scenario);

        IEmployeeHomeQueries queries = scope.ServiceProvider.GetRequiredService<IEmployeeHomeQueries>();
        int count = await queries.CountPendingReviewerAssignmentsAsync(scenario.ReviewerId, CancellationToken.None);

        Assert.Equal(1, count);
    }

    [SkippableFact]
    public async Task HasAnyReviewerDiplomasAsync_AfterAssignment_ReturnsTrue()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToReviewerAssignmentStepAsync(services, scenario);
        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        await secretaryActions.AssignReviewerAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new AssignReviewerDto(scenario.DiplomaId, scenario.ReviewerId),
            CancellationToken.None);

        IEmployeeHomeQueries queries = services.GetRequiredService<IEmployeeHomeQueries>();
        bool hasDiplomas = await queries.HasAnyReviewerDiplomasAsync(scenario.ReviewerId, null, CancellationToken.None);

        Assert.True(hasDiplomas);
    }
}
