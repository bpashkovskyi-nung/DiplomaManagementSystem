using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class CheckpointRejectionScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task SupervisorRejectsFeedback_StudentRetriesAndPasses()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using (AsyncServiceScope prepScope = fixture.CreateProvider().CreateAsyncScope())
        {
            await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(
                prepScope.ServiceProvider,
                scenario);
        }

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        IAdmissionReviewService admissionReviewService = services.GetRequiredService<IAdmissionReviewService>();
        await admissionReviewService.CompleteSupervisorFeedbackAsync(
            scenario.SupervisorId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.NotApproved, "Доробити вступ"),
            null,
            CancellationToken.None);

        DiplomaDetailsDto rejectedDetails = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(
            services,
            scenario);
        Assert.Equal(AdmissionStep.SupervisorFeedback, rejectedDetails.State.CurrentAdmissionStep);
        Assert.Contains(
            rejectedDetails.History.AttemptHistory,
            attempt => attempt.Step == AdmissionStep.SupervisorFeedback
                       && attempt.Outcome == CheckpointOutcome.NotApproved);

        await admissionReviewService.CompleteSupervisorFeedbackAsync(
            scenario.SupervisorId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            IntegrationTestDocuments.CreatePdf("supervisor-feedback-retry.pdf"),
            CancellationToken.None);

        DiplomaDetailsDto approvedDetails = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(
            services,
            scenario);
        Assert.Equal(AdmissionStep.FormattingReview, approvedDetails.State.CurrentAdmissionStep);
        Assert.Equal(2, approvedDetails.History.AttemptHistory.Count(
            attempt => attempt.Step == AdmissionStep.SupervisorFeedback));
    }
}
