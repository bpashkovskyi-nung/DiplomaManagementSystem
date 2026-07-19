using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class DocumentAttemptLinkScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task CheckpointUpload_LinksDocumentToAdmissionStepAttempt()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(services, scenario);
        await WorkflowScenarioRunner.RunUpToReadyForAdmissionAsync(services, scenario);

        IDiplomaDocumentQueries documentQueries = services.GetRequiredService<IDiplomaDocumentQueries>();
        List<DiplomaDocument> documents = await documentQueries.ListForDiplomaReadAsync(
            scenario.DiplomaId,
            CancellationToken.None);

        var checkpointDocuments = documents
            .Where(document => document.AdmissionStepAttemptId is not null)
            .ToList();

        Assert.NotEmpty(checkpointDocuments);
        Assert.Contains(
            checkpointDocuments,
            document => document.Kind == DiplomaDocumentKind.SupervisorFeedback);
        Assert.Contains(
            checkpointDocuments,
            document => document.Kind == DiplomaDocumentKind.AntiPlagiarismReport);
        Assert.Contains(
            checkpointDocuments,
            document => document.Kind == DiplomaDocumentKind.ExternalReview);

        foreach (DiplomaDocument document in checkpointDocuments)
        {
            Assert.NotEqual(Guid.Empty, document.AdmissionStepAttemptId);
        }
    }
}
