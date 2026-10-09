using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class ArchivedSessionScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task ArchivedSession_BlocksWrites()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IDefenceSessionService defenceSessionService = scope.ServiceProvider.GetRequiredService<IDefenceSessionService>();
        ISecretaryDiplomaActionService secretaryActions = scope.ServiceProvider.GetRequiredService<ISecretaryDiplomaActionService>();

        await defenceSessionService.ArchiveAsync(scenario.SessionId, scenario.SecretaryId);

        await Assert.ThrowsAsync<DomainException>(() =>
            secretaryActions.AddCommentAsync(
                scenario.SecretaryId,
                scenario.SessionId,
                new AddCommentDto(scenario.DiplomaId, "Should fail"),
                CancellationToken.None));
    }

    [SkippableFact]
    public async Task ArchivedSession_BlocksStudentUpload()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToReviewerAssignedAsync(services, scenario);

        IDefenceSessionService defenceSessionService = services.GetRequiredService<IDefenceSessionService>();
        await defenceSessionService.ArchiveAsync(scenario.SessionId, scenario.SecretaryId);

        IStudentDiplomaService studentService = services.GetRequiredService<IStudentDiplomaService>();
        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            studentService.UploadWorkAsync(
                scenario.StudentId,
                scenario.DiplomaId,
                IntegrationTestDocuments.CreatePdf("work.pdf"),
                CancellationToken.None));

        Assert.Contains("archived", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task ArchivedSession_BlocksDeclareWorkReady()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToReviewerAssignedAsync(services, scenario);
        await WorkflowScenarioRunner.UploadStudentWorkAsync(services, scenario);

        IDefenceSessionService defenceSessionService = services.GetRequiredService<IDefenceSessionService>();
        await defenceSessionService.ArchiveAsync(scenario.SessionId, scenario.SecretaryId);

        IStudentDiplomaService studentService = services.GetRequiredService<IStudentDiplomaService>();
        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            studentService.DeclareWorkReadyAsync(
                scenario.StudentId,
                scenario.DiplomaId,
                CancellationToken.None));

        Assert.Contains("archived", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
