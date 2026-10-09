using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class AdmissionCheckpointEdgeScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task SupervisorFeedback_WithoutDocument_ThrowsDomainException()
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
                scenario.SupervisorId,
                new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
                IntegrationTestDocuments.CreateEmptyPdf("feedback.pdf"),
                CancellationToken.None));

        Assert.Contains("порожній", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task FormattingBeforeSupervisorFeedback_ThrowsDomainException()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(services, scenario);

        IAdmissionReviewService admissionReviewService = services.GetRequiredService<IAdmissionReviewService>();
        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            admissionReviewService.CompleteFormattingReviewAsync(
                scenario.FormattingId,
                new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
                CancellationToken.None));

        Assert.Contains("current admission step", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task FindWritableAsync_WhenAttemptTracked_ReturnsAttempt()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToFormattingReviewStepAsync(services, scenario);

        IApplicationDbContext dbContext = services.GetRequiredService<IApplicationDbContext>();
        Domain.Entities.Diploma diploma = await dbContext.Diplomas
            .Include(item => item.AdmissionStepAttempts)
            .FirstAsync(item => item.Id == scenario.DiplomaId);

        Guid attemptId = Assert.Single(diploma.AdmissionStepAttempts).Id;

        IAdmissionStepQueries admissionStepQueries = services.GetRequiredService<IAdmissionStepQueries>();
        Domain.Entities.DiplomaAdmissionStepAttempt? found = await admissionStepQueries.FindWritableAsync(
            scenario.DiplomaId,
            attemptId,
            CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(attemptId, found.Id);
    }
}
