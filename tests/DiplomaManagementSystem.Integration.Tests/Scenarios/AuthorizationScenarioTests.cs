using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class AuthorizationScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task AssignReviewer_WithoutApprovedTopic_ThrowsDomainException()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);

        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            secretaryActions.AssignReviewerAsync(
                scenario.SecretaryId,
                scenario.SessionId,
                new AssignReviewerDto(scenario.DiplomaId, scenario.ReviewerId),
                CancellationToken.None));

        Assert.Contains("topic is approved", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task FormattingReviewer_CompletesSupervisorCheckpoint_ThrowsNotSupervisor()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(services, scenario);

        IAdmissionReviewService admissionReviewService = services.GetRequiredService<IAdmissionReviewService>();
        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            admissionReviewService.CompleteSupervisorFeedbackAsync(
                scenario.FormattingId,
                new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
                IntegrationTestDocuments.CreatePdf("wrong-role.pdf"),
                CancellationToken.None));

        Assert.Equal(AuthorizationMessages.NotSupervisor, exception.Message);
    }
}
