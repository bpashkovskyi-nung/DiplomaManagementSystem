using System.Net;

using DiplomaManagementSystem.Integration.Tests.Support;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AdminStudentsDetailsEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetDetails_ShowsStudent()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync($"/Admin/Students/Details/{scenario.StudentId}");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Student One");
    }

    [SkippableFact]
    public async Task GetDelete_ShowsConfirmation()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        StudentOnlyScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedStudentWithoutDiplomaAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync($"/Admin/Students/Delete/{scenario.StudentId}");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Student Without Diploma");
    }

    [SkippableFact]
    public async Task PostDelete_RemovesStudentWithoutDiploma()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        StudentOnlyScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedStudentWithoutDiplomaAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage deletePage = await client.GetAsync($"/Admin/Students/Delete/{scenario.StudentId}");
        deletePage.EnsureSuccessStatusCode();
        string deleteHtml = await deletePage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(deleteHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = scenario.StudentId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync(
            $"/Admin/Students/Delete/{scenario.StudentId}",
            form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        HttpResponseMessage listPage = await client.GetAsync(
            $"/Admin/Students?defenceSessionId={scenario.SessionId}");
        listPage.EnsureSuccessStatusCode();
        string listHtml = await listPage.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Student Without Diploma", listHtml, StringComparison.Ordinal);
    }
}
