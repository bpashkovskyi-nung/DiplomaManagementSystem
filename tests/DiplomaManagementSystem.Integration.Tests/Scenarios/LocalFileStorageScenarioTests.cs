using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Application.Storage.Contracts;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class LocalFileStorageScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task UploadFileAsync_DuplicateFileName_AppendsNumericSuffix()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        await new IntegrationScenarioBuilder(fixture.CreateProvider()).SeedSessionOnlyAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IFileStorageService fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();

        string folderId = await fileStorage.EnsureFolderAsync(null, $"uploads-{Guid.NewGuid():N}", CancellationToken.None);
        UploadFileContent first = IntegrationTestDocuments.CreatePdf("report.pdf");
        UploadFileContent second = IntegrationTestDocuments.CreatePdf("report.pdf");

        StoredFileResult firstResult = await fileStorage.UploadFileAsync(folderId, "report.pdf", first, CancellationToken.None);
        StoredFileResult secondResult = await fileStorage.UploadFileAsync(folderId, "report.pdf", second, CancellationToken.None);

        Assert.Equal("report.pdf", firstResult.FileName);
        Assert.Equal("report_2.pdf", secondResult.FileName);
        Assert.NotEqual(firstResult.FileId, secondResult.FileId);
    }
}
