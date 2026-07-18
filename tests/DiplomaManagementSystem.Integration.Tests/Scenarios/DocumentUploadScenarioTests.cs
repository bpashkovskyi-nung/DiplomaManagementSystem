using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class DocumentUploadScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task UploadWork_InvalidFileType_ThrowsDomainException()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToReviewerAssignedAsync(services, scenario);

        IStudentDiplomaService studentService = services.GetRequiredService<IStudentDiplomaService>();
        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            studentService.UploadWorkAsync(
                scenario.StudentId,
                scenario.DiplomaId,
                IntegrationTestDocuments.CreateInvalid("work.exe", "application/octet-stream"),
                CancellationToken.None));

        Assert.Contains("формати", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
