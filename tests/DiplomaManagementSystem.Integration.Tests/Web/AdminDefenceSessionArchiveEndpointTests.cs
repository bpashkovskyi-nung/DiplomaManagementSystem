using System.Net;

using DiplomaManagementSystem.Integration.Tests.Support;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AdminDefenceSessionArchiveEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostCreate_WithInvalidYear_RedisplaysForm()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage formPage = await client.GetAsync("/Admin/DefenceSessions/Create");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Year"] = "1999",
            ["Type"] = "Bachelor",
            ["Semester"] = "1",
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Admin/DefenceSessions/Create", form);

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        string html = await postResponse.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Нова сесія захисту");
        IntegrationTestHtmlAssertions.AssertContainsText(html, "між 2000 та 2100", ignoreCase: true);
    }

    [SkippableFact]
    public async Task PostArchive_ArchivesSessionAndShowsOnDetails()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid sessionId = await new IntegrationScenarioBuilder(fixture.CreateProvider()).SeedSessionOnlyAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage detailsPage = await client.GetAsync($"/Admin/DefenceSessions/Details/{sessionId}");
        detailsPage.EnsureSuccessStatusCode();
        string detailsHtml = await detailsPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(detailsHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["id"] = sessionId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync(
            $"/Admin/DefenceSessions/Archive/{sessionId}",
            form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        HttpResponseMessage archivedPage = await client.GetAsync($"/Admin/DefenceSessions/Details/{sessionId}");
        archivedPage.EnsureSuccessStatusCode();
        string archivedHtml = await archivedPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(archivedHtml, "заархівовано", ignoreCase: true);
    }
}
