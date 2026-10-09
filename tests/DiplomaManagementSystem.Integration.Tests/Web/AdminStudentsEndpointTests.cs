using System.Net;

using DiplomaManagementSystem.Integration.Tests.Support;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AdminStudentsEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetIndex_ShowsStudentsForSession()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync(
            $"/Admin/Students?defenceSessionId={scenario.SessionId}");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Student One");
    }

    [SkippableFact]
    public async Task PostCreate_AddsStudentAndRedirectsToList()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string email = $"student.web.{suffix}@test.local";

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage formPage = await client.GetAsync(
            $"/Admin/Students/Create?defenceSessionId={scenario.SessionId}");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DefenceSessionId"] = scenario.SessionId.ToString(),
            ["FullName"] = "Web Created Student",
            ["Email"] = email,
            ["StudyGroupId"] = scenario.GroupId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Admin/Students/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            $"/Admin/Students?defenceSessionId={scenario.SessionId}",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage listPage = await client.GetAsync(postResponse.Headers.Location!);
        listPage.EnsureSuccessStatusCode();
        string listHtml = await listPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(listHtml, "Web Created Student");
        IntegrationTestHtmlAssertions.AssertContainsText(listHtml, email);
    }
}
