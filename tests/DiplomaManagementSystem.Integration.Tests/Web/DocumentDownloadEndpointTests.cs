using System.Net;

using DiplomaManagementSystem.Application.Documents.Contracts;
using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Infrastructure.Storage;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class DocumentDownloadEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetDownload_Authenticated_ReturnsStoredFileBytes()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToReviewerAssignedAsync(setupScope.ServiceProvider, scenario);
        await WorkflowScenarioRunner.UploadStudentWorkAsync(setupScope.ServiceProvider, scenario);

        IDiplomaDocumentService documentService = setupScope.ServiceProvider.GetRequiredService<IDiplomaDocumentService>();
        DiplomaDocumentsBundleDto bundle = await documentService.GetDocumentsAsync(
            scenario.DiplomaId,
            CancellationToken.None);
        DiplomaDocumentDto document = Assert.Single(bundle.StudentWorkVersions);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.StudentId);

        HttpResponseMessage response = await client.GetAsync(document.ViewUrl);

        response.EnsureSuccessStatusCode();
        Assert.Equal("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(
            document.FileName,
            response.Content.Headers.ContentDisposition?.FileName,
            StringComparison.OrdinalIgnoreCase);

        byte[] body = await response.Content.ReadAsByteArrayAsync();
        Assert.Contains("test document content"u8.ToArray(), body);
    }

    [SkippableFact]
    public async Task GetDownload_Unauthenticated_RedirectsToLogin()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToReviewerAssignedAsync(setupScope.ServiceProvider, scenario);
        await WorkflowScenarioRunner.UploadStudentWorkAsync(setupScope.ServiceProvider, scenario);

        IDiplomaDocumentService documentService = setupScope.ServiceProvider.GetRequiredService<IDiplomaDocumentService>();
        DiplomaDocumentDto document = Assert.Single(
            (await documentService.GetDocumentsAsync(scenario.DiplomaId, CancellationToken.None)).StudentWorkVersions);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory);

        HttpResponseMessage response = await client.GetAsync(document.ViewUrl);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task GetDownload_MissingFile_ReturnsNotFound()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        string missingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.pdf");
        string encodedPath = Uri.EscapeDataString(LocalFilePathCodec.EncodePath(missingPath));

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.StudentId);

        HttpResponseMessage response = await client.GetAsync($"/local-files/{encodedPath}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
