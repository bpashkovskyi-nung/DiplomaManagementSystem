using System.Net;
using System.Net.Http.Headers;

using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SupervisorCheckpointEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostCompleteCheckpoint_RedirectsAndAdvancesAdmissionStep()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SupervisorId);

        HttpResponseMessage checkpointsPage = await client.GetAsync("/Employee/Supervisor/Checkpoints");
        checkpointsPage.EnsureSuccessStatusCode();
        string html = await checkpointsPage.Content.ReadAsStringAsync();
        Assert.Contains("Student One", html, StringComparison.Ordinal);
        IntegrationTestHtmlAssertions.AssertContainsText(html, "v1");

        string token = AntiforgeryTokenParser.Parse(html);
        byte[] documentBytes = "test document content"u8.ToArray();
        MultipartFormDataContent form = new();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new StringContent(scenario.DiplomaId.ToString()), "DiplomaId");
        form.Add(new StringContent(((int)CheckpointOutcome.Approved).ToString()), "Outcome");
        ByteArrayContent fileContent = new(documentBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "Document", "supervisor-feedback.pdf");

        HttpResponseMessage postResponse = await client.PostAsync("/Employee/Supervisor/CompleteCheckpoint", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Employee/Supervisor/Checkpoints",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        DiplomaDetailsDto? details = await verifyScope.ServiceProvider
            .GetRequiredService<ISecretaryDiplomaDetailsService>()
            .GetDetailsAsync(scenario.SessionId, scenario.DiplomaId, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(AdmissionStep.FormattingReview, details.State.CurrentAdmissionStep);
    }
}
