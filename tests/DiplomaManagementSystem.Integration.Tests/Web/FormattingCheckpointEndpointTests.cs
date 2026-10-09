using System.Net;

using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class FormattingCheckpointEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostComplete_RedirectsAndAdvancesAdmissionStep()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToFormattingReviewStepAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.FormattingId);

        HttpResponseMessage pageResponse = await client.GetAsync("/Employee/FormattingReview/Pending");
        pageResponse.EnsureSuccessStatusCode();
        string html = await pageResponse.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "v1");
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DiplomaId"] = scenario.DiplomaId.ToString(),
            ["Outcome"] = ((int)CheckpointOutcome.Approved).ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Employee/FormattingReview/Complete", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Employee/FormattingReview/Pending",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        DiplomaDetailsDto? details = await verifyScope.ServiceProvider
            .GetRequiredService<ISecretaryDiplomaDetailsService>()
            .GetDetailsAsync(scenario.SessionId, scenario.DiplomaId, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(AdmissionStep.AntiPlagiarismClearance, details.State.CurrentAdmissionStep);
    }
}
