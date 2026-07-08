using System.Net;
using DiplomaManagementSystem.Integration.Tests.Support;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AdminPreviewEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostSetSecretaryMode_RedirectsToSelectUser()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage homeResponse = await client.GetAsync("/SuperAdmin/Home/Index");
        homeResponse.EnsureSuccessStatusCode();
        string html = await homeResponse.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["mode"] = "1",
            ["returnUrl"] = "/",
        });

        HttpResponseMessage postResponse = await client.PostAsync("/AdminPreview/Set", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        string? location = postResponse.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/AdminPreview/SelectUser", location, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mode=Secretary", location, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task PostSetUser_RedirectsToSecretaryDashboard()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage superAdminHome = await client.GetAsync("/SuperAdmin/Home/Index");
        superAdminHome.EnsureSuccessStatusCode();
        string adminHtml = await superAdminHome.Content.ReadAsStringAsync();
        string setToken = AntiforgeryTokenParser.Parse(adminHtml);

        FormUrlEncodedContent setForm = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = setToken,
            ["mode"] = "1",
            ["returnUrl"] = "/",
        });

        HttpResponseMessage setResponse = await client.PostAsync("/AdminPreview/Set", setForm);
        Assert.Equal(HttpStatusCode.Redirect, setResponse.StatusCode);
        string? selectUserPath = setResponse.Headers.Location?.ToString();
        Assert.NotNull(selectUserPath);

        HttpResponseMessage selectUserResponse = await client.GetAsync(selectUserPath);
        selectUserResponse.EnsureSuccessStatusCode();
        string selectUserHtml = await selectUserResponse.Content.ReadAsStringAsync();
        string setUserToken = AntiforgeryTokenParser.Parse(selectUserHtml);

        FormUrlEncodedContent setUserForm = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = setUserToken,
            ["userId"] = scenario.SecretaryId.ToString(),
            ["returnUrl"] = "/",
        });

        HttpResponseMessage setUserResponse = await client.PostAsync("/AdminPreview/SetUser", setUserForm);

        Assert.Equal(HttpStatusCode.Redirect, setUserResponse.StatusCode);
        Assert.Contains(
            "/Secretary/Dashboard",
            setUserResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }
}
