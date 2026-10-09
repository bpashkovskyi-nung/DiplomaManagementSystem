using System.Net;

using DiplomaManagementSystem.Integration.Tests.Support;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AdminDefenceSessionsEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetCreate_ShowsForm()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync("/Admin/DefenceSessions/Create");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Нова сесія захисту");
    }

    [SkippableFact]
    public async Task PostCreate_AddsSessionAndRedirectsToIndex()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        int year = Random.Shared.Next(2080, 2099);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage formPage = await client.GetAsync("/Admin/DefenceSessions/Create");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Year"] = year.ToString(),
            ["Type"] = "Bachelor",
            ["Semester"] = "1",
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Admin/DefenceSessions/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Admin/DefenceSessions",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage indexPage = await client.GetAsync("/Admin/DefenceSessions");
        indexPage.EnsureSuccessStatusCode();
        string indexHtml = await indexPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(indexHtml, year.ToString());
    }

    [SkippableFact]
    public async Task GetDetails_ShowsSessionDetails()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync(
            $"/Admin/DefenceSessions/Details/{scenario.SessionId}");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, scenario.StudyGroupName);
    }

    [SkippableFact]
    public async Task PostEdit_UpdatesSessionAndRedirectsToDetails()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        int year = Random.Shared.Next(2080, 2099);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage formPage = await client.GetAsync($"/Admin/DefenceSessions/Edit/{scenario.SessionId}");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = scenario.SessionId.ToString(),
            ["Year"] = year.ToString(),
            ["Type"] = "Bachelor",
            ["Semester"] = "2",
        });

        HttpResponseMessage postResponse = await client.PostAsync(
            $"/Admin/DefenceSessions/Edit/{scenario.SessionId}",
            form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            $"/Admin/DefenceSessions/Details/{scenario.SessionId}",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage detailsPage = await client.GetAsync(postResponse.Headers.Location!);
        detailsPage.EnsureSuccessStatusCode();
        string detailsHtml = await detailsPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(detailsHtml, year.ToString());
    }
}
