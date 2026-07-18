using System.Net;
using System.Net.Http.Headers;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AntiPlagiarismCheckpointEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostComplete_RedirectsAndAdvancesAdmissionStep()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToAntiPlagiarismStepAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.AntiPlagiarismId);

        HttpResponseMessage pageResponse = await client.GetAsync("/Employee/AntiPlagiarism/Pending");
        pageResponse.EnsureSuccessStatusCode();
        string html = await pageResponse.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        byte[] documentBytes = "anti-plagiarism report"u8.ToArray();
        MultipartFormDataContent form = new();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new StringContent(scenario.DiplomaId.ToString()), "DiplomaId");
        form.Add(new StringContent(((int)CheckpointOutcome.Approved).ToString()), "Outcome");
        ByteArrayContent fileContent = new(documentBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "Document", "anti-plagiarism.pdf");

        HttpResponseMessage postResponse = await client.PostAsync("/Employee/AntiPlagiarism/Complete", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Employee/AntiPlagiarism/Pending",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        DiplomaDetailsDto? details = await verifyScope.ServiceProvider
            .GetRequiredService<ISecretaryDiplomaDetailsService>()
            .GetDetailsAsync(scenario.SessionId, scenario.DiplomaId, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(AdmissionStep.ExternalReview, details.State.CurrentAdmissionStep);
    }
}
