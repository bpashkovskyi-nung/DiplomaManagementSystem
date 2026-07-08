using System.Net;

using DiplomaManagementSystem.Integration.Tests.Support;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class MultiTenancyEndpointTests(PostgreSqlFixture fixture)
{
    private static int NextUniqueYear() => Random.Shared.Next(3000, 3999);

    [SkippableFact]
    public async Task AdminDefenceSessions_AreIsolatedByDepartment()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        Guid defaultAdminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        int otherYear = NextUniqueYear();
        SecondDepartmentSeed second =
            await IntegrationDepartmentHelper.CreateSecondDepartmentWithAdminAndSessionAsync(
                fixture.CreateProvider(),
                otherYear);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();

        HttpClient secondAdminClient = IntegrationTestWebClient.CreateClient(factory, second.AdminId);
        HttpResponseMessage secondResponse = await secondAdminClient.GetAsync("/Admin/DefenceSessions");
        secondResponse.EnsureSuccessStatusCode();
        string secondHtml = await secondResponse.Content.ReadAsStringAsync();
        Assert.Contains(otherYear.ToString(), secondHtml, StringComparison.Ordinal);

        HttpClient defaultAdminClient = IntegrationTestWebClient.CreateClient(factory, defaultAdminId);
        HttpResponseMessage defaultResponse = await defaultAdminClient.GetAsync("/Admin/DefenceSessions");
        defaultResponse.EnsureSuccessStatusCode();
        string defaultHtml = await defaultResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(otherYear.ToString(), defaultHtml, StringComparison.Ordinal);
    }

    [SkippableFact]
    public async Task SuperAdmin_EnterDepartment_GrantsAdminAccessViaCookie()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        Guid departmentId = await IntegrationDepartmentHelper.GetDefaultDepartmentIdAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage listPage = await client.GetAsync("/SuperAdmin/Departments");
        listPage.EnsureSuccessStatusCode();
        string listHtml = await listPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(listHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["id"] = departmentId.ToString(),
        });

        HttpResponseMessage enterResponse = await client.PostAsync("/SuperAdmin/Departments/Enter", form);
        Assert.Equal(HttpStatusCode.Redirect, enterResponse.StatusCode);
        Assert.Contains("/Admin", enterResponse.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage adminResponse = await client.GetAsync("/Admin/DefenceSessions");
        adminResponse.EnsureSuccessStatusCode();
    }

    [SkippableFact]
    public async Task DualRoleUser_CanAccessBothSuperAdminAndAdminAreas()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        Guid userId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(
            fixture.CreateProvider(),
            alsoDepartmentAdmin: true);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, userId);

        HttpResponseMessage superAdminResponse = await client.GetAsync("/SuperAdmin/Home");
        superAdminResponse.EnsureSuccessStatusCode();

        HttpResponseMessage adminResponse = await client.GetAsync("/Admin/DefenceSessions");
        adminResponse.EnsureSuccessStatusCode();
    }
}
