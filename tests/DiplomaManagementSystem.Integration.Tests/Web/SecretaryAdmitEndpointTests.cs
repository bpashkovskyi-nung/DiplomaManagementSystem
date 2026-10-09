using System.Net;

using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretaryAdmitEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostAdmit_RedirectsAndAdmitsDiploma()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(setupScope.ServiceProvider, scenario);
        await WorkflowScenarioRunner.RunUpToReadyForAdmissionAsync(setupScope.ServiceProvider, scenario);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage tokenPageResponse = await client.GetAsync("/Employee/Home/Index");
        tokenPageResponse.EnsureSuccessStatusCode();
        string html = await tokenPageResponse.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DiplomaId"] = scenario.DiplomaId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Secretary/Diplomas/Admit", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            $"/Secretary/Diplomas/Details/{scenario.DiplomaId}",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        DiplomaDetailsDto? details = await verifyScope.ServiceProvider
            .GetRequiredService<ISecretaryDiplomaDetailsService>()
            .GetDetailsAsync(scenario.SessionId, scenario.DiplomaId, CancellationToken.None);

        Assert.NotNull(details);
        IntegrationScenarioAssertions.AssertAdmitted(details);
        Assert.Null(details.State.DefenceDate);
    }
}
