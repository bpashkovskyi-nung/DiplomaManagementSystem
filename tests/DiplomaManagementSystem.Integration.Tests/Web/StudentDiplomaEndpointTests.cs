using System.Net;
using System.Net.Http.Headers;

using DiplomaManagementSystem.Application.Documents.Contracts;
using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class StudentDiplomaEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostUploadWork_RedirectsAndStoresDocument()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToReviewerAssignedAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.StudentId);

        HttpResponseMessage indexResponse = await client.GetAsync("/Student/Diploma");
        indexResponse.EnsureSuccessStatusCode();
        string html = await indexResponse.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        byte[] documentBytes = "test document content"u8.ToArray();
        MultipartFormDataContent form = new();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new StringContent(scenario.DiplomaId.ToString()), "DiplomaId");
        ByteArrayContent fileContent = new(documentBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "WorkFile", "student-work.pdf");

        HttpResponseMessage postResponse = await client.PostAsync("/Student/Diploma/UploadWork", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains("/Student/Diploma", postResponse.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        IDiplomaDocumentService documentService = verifyScope.ServiceProvider.GetRequiredService<IDiplomaDocumentService>();
        IStudentDiplomaService studentService = verifyScope.ServiceProvider.GetRequiredService<IStudentDiplomaService>();
        DiplomaDocumentsBundleDto bundle = await documentService.GetDocumentsAsync(scenario.DiplomaId, CancellationToken.None);
        MyDiplomaDto diploma = await studentService.GetMyDiplomaAsync(scenario.StudentId, CancellationToken.None);

        Assert.Single(bundle.StudentWorkVersions);
        Assert.Equal(DiplomaLifecycleStatus.ReviewerAssigned, diploma.State!.LifecycleStatus);
        Assert.Null(diploma.State.CurrentAdmissionStep);
    }

    [SkippableFact]
    public async Task PostDeclareWorkReady_RedirectsAndStartsAdmissionReview()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToReviewerAssignedAsync(setupScope.ServiceProvider, scenario);
        await WorkflowScenarioRunner.UploadStudentWorkAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.StudentId);

        HttpResponseMessage indexResponse = await client.GetAsync("/Student/Diploma");
        indexResponse.EnsureSuccessStatusCode();
        string token = AntiforgeryTokenParser.Parse(await indexResponse.Content.ReadAsStringAsync());

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Student/Diploma/DeclareWorkReady", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains("/Student/Diploma", postResponse.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        IStudentDiplomaService studentService = verifyScope.ServiceProvider.GetRequiredService<IStudentDiplomaService>();
        MyDiplomaDto diploma = await studentService.GetMyDiplomaAsync(scenario.StudentId, CancellationToken.None);

        Assert.Equal(DiplomaLifecycleStatus.DocumentsInProgress, diploma.State!.LifecycleStatus);
        Assert.Equal(AdmissionStep.SupervisorFeedback, diploma.State.CurrentAdmissionStep);
    }
}
