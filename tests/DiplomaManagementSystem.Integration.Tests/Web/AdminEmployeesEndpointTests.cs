using System.Net;

using DiplomaManagementSystem.Integration.Tests.Support;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AdminEmployeesEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetIndex_ShowsEmployees()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync("/Admin/Employees");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Supervisor One");
        Assert.NotEqual(Guid.Empty, scenario.SupervisorId);
    }

    [SkippableFact]
    public async Task PostCreate_AddsEmployeeAndRedirectsToIndex()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string fullName = $"Employee Web {suffix}";
        string email = $"employee.web.{suffix}@test.local";

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage formPage = await client.GetAsync("/Admin/Employees/Create");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FullName"] = fullName,
            ["Email"] = email,
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Admin/Employees/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Admin/Employees",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage indexPage = await client.GetAsync("/Admin/Employees");
        indexPage.EnsureSuccessStatusCode();
        string indexHtml = await indexPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(indexHtml, fullName);
        IntegrationTestHtmlAssertions.AssertContainsText(indexHtml, email);
    }

    [SkippableFact]
    public async Task GetDetails_ShowsEmployee()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync($"/Admin/Employees/Details/{scenario.SupervisorId}");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Supervisor One");
    }

    [SkippableFact]
    public async Task PostEdit_UpdatesEmployeeAndRedirectsToDetails()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string updatedName = $"Supervisor Updated {suffix}";

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage formPage = await client.GetAsync($"/Admin/Employees/Edit/{scenario.SupervisorId}");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = scenario.SupervisorId.ToString(),
            ["FullName"] = updatedName,
            ["Email"] = $"supervisor.updated.{suffix}@test.local",
        });

        HttpResponseMessage postResponse = await client.PostAsync(
            $"/Admin/Employees/Edit/{scenario.SupervisorId}",
            form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            $"/Admin/Employees/Details/{scenario.SupervisorId}",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage detailsPage = await client.GetAsync(postResponse.Headers.Location!);
        detailsPage.EnsureSuccessStatusCode();
        string detailsHtml = await detailsPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(detailsHtml, updatedName);
    }
}
