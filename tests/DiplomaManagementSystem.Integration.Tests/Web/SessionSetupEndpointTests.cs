using System.Net;

using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SessionSetupEndpointTests(PostgreSqlFixture fixture)
{
    // TC-INT-F8-HTTP-001
    [SkippableFact]
    public async Task PostSaveMilestones_RedirectsAndPersistsMilestones()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage tokenPageResponse = await client.GetAsync("/Secretary/SessionSetup/Index");
        tokenPageResponse.EnsureSuccessStatusCode();
        string html = await tokenPageResponse.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Milestones[0].DueDate"] = "2026-03-01",
            ["Milestones[0].ExpectedPercent"] = "30",
            ["Milestones[1].DueDate"] = "2026-04-01",
            ["Milestones[1].ExpectedPercent"] = "60",
            ["Milestones[2].DueDate"] = "2026-05-01",
            ["Milestones[2].ExpectedPercent"] = "100",
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Secretary/SessionSetup/SaveMilestones", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Secretary/SessionSetup",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        ISessionSetupService sessionSetupService =
            verifyScope.ServiceProvider.GetRequiredService<ISessionSetupService>();
        SessionSetupPageDto? page = await sessionSetupService.GetPageAsync(scenario.SessionId, CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal(3, page.Milestones.Count);
        Assert.Equal(new DateOnly(2026, 3, 1), page.Milestones[0].DueDate);
        Assert.Equal(30, page.Milestones[0].ExpectedPercent);
        Assert.Equal(new DateOnly(2026, 4, 1), page.Milestones[1].DueDate);
        Assert.Equal(60, page.Milestones[1].ExpectedPercent);
        Assert.Equal(new DateOnly(2026, 5, 1), page.Milestones[2].DueDate);
        Assert.Equal(100, page.Milestones[2].ExpectedPercent);
    }

    // TC-INT-F8-HTTP-002
    [SkippableFact]
    public async Task PostSaveMilestones_InvalidCount_ShowsErrorAndDoesNotPersist()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage tokenPageResponse = await client.GetAsync("/Secretary/SessionSetup/Index");
        tokenPageResponse.EnsureSuccessStatusCode();
        string html = await tokenPageResponse.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Milestones[0].DueDate"] = "2026-03-01",
            ["Milestones[0].ExpectedPercent"] = "30",
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Secretary/SessionSetup/SaveMilestones", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        ISessionSetupService sessionSetupService =
            verifyScope.ServiceProvider.GetRequiredService<ISessionSetupService>();
        SessionSetupPageDto? page = await sessionSetupService.GetPageAsync(scenario.SessionId, CancellationToken.None);

        Assert.NotNull(page);
        Assert.All(page.Milestones, milestone => Assert.Null(milestone.Id));
    }

    // TC-INT-F8-HTTP-003
    [SkippableFact]
    public async Task PostSaveDefenceDates_RedirectsAndPersistsDates()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage tokenPageResponse = await client.GetAsync("/Secretary/SessionSetup/Index");
        tokenPageResponse.EnsureSuccessStatusCode();
        string html = await tokenPageResponse.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["dates[0]"] = "2026-06-20",
            ["dates[1]"] = "2026-06-27",
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Secretary/SessionSetup/SaveDefenceDates", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        ISessionSetupService sessionSetupService =
            verifyScope.ServiceProvider.GetRequiredService<ISessionSetupService>();
        SessionSetupPageDto? page = await sessionSetupService.GetPageAsync(scenario.SessionId, CancellationToken.None);

        Assert.NotNull(page);
        Assert.Equal(2, page.AvailableDates.Count);
        Assert.Contains(page.AvailableDates, option => option.Date == new DateOnly(2026, 6, 20));
        Assert.Contains(page.AvailableDates, option => option.Date == new DateOnly(2026, 6, 27));
    }
}
