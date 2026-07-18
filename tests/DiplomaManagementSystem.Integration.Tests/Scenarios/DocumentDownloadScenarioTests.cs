using DiplomaManagementSystem.Application.Documents.Contracts;
using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Infrastructure.Storage;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class DocumentDownloadScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task UploadStudentWork_ExposesLocalFileViewUrlAndStoredFile()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToReviewerAssignedAsync(services, scenario);
        await WorkflowScenarioRunner.UploadStudentWorkAsync(services, scenario);

        IDiplomaDocumentService documentService = services.GetRequiredService<IDiplomaDocumentService>();
        DiplomaDocumentsBundleDto bundle = await documentService.GetDocumentsAsync(
            scenario.DiplomaId,
            CancellationToken.None);

        DiplomaDocumentDto document = Assert.Single(bundle.StudentWorkVersions);
        Assert.StartsWith("/local-files/", document.ViewUrl, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(document.FileName));
        Assert.True(document.SizeBytes > 0);

        string encodedSegment = document.ViewUrl["/local-files/".Length..];
        string filePath = LocalFilePathCodec.DecodePath(Uri.UnescapeDataString(encodedSegment));
        Assert.True(File.Exists(filePath));

        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
        Assert.Contains("test document content"u8.ToArray(), fileBytes);
    }
}
