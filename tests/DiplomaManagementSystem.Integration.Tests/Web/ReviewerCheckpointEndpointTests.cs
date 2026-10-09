using System.Net;
using System.Net.Http.Headers;

using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class ReviewerCheckpointEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostComplete_RedirectsAndAdvancesAdmissionStep()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToExternalReviewStepAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.ReviewerId);

        HttpResponseMessage pageResponse = await client.GetAsync("/Employee/Reviewer/Assignments");
        pageResponse.EnsureSuccessStatusCode();
        string html = await pageResponse.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        byte[] documentBytes = "test document content"u8.ToArray();
        MultipartFormDataContent form = new();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new StringContent(scenario.DiplomaId.ToString()), "DiplomaId");
        form.Add(new StringContent(((int)CheckpointOutcome.Approved).ToString()), "Outcome");
        form.Add(new StringContent("OK"), "Comment");
        ByteArrayContent fileContent = new(documentBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "Document", "external-review.pdf");

        HttpResponseMessage postResponse = await client.PostAsync("/Employee/Reviewer/Complete", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Employee/Reviewer/Assignments",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        DiplomaDetailsDto? details = await verifyScope.ServiceProvider
            .GetRequiredService<ISecretaryDiplomaDetailsService>()
            .GetDetailsAsync(scenario.SessionId, scenario.DiplomaId, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(DiplomaLifecycleStatus.ReadyForAdmission, details.State.LifecycleStatus);
    }
}
