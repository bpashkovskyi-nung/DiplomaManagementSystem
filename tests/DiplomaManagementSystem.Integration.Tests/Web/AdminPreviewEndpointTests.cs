using System.Net;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

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

    [SkippableFact]
    public async Task SelectUser_AfterEnterDepartment_ShowsOnlyScopedEmployees()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        Guid departmentId = await IntegrationDepartmentHelper.GetDefaultDepartmentIdAsync(fixture.CreateProvider());
        Guid facultyId = await IntegrationDepartmentHelper.GetDefaultFacultyIdAsync(fixture.CreateProvider());

        int otherYear = Random.Shared.Next(4000, 4999);
        SecondDepartmentSeed secondDepartment =
            await IntegrationDepartmentHelper.CreateSecondDepartmentWithAdminAndSessionAsync(
                fixture.CreateProvider(),
                otherYear);

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        IUserProvisioningService userProvisioning =
            setupScope.ServiceProvider.GetRequiredService<IUserProvisioningService>();
        IApplicationDbContext dbContext =
            setupScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        string suffix = Guid.NewGuid().ToString("N")[..6];
        ApplicationUser otherEmployee = await userProvisioning.CreateEmployeeAsync(
            $"Other Dept Employee {suffix}",
            $"other.dept.{suffix}@test.local");
        dbContext.DepartmentEmployees.Add(new DepartmentEmployee
        {
            Id = Guid.NewGuid(),
            DepartmentId = secondDepartment.DepartmentId,
            UserId = otherEmployee.Id,
            FullName = otherEmployee.FullName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage departmentsPage = await client.GetAsync($"/SuperAdmin/Departments?facultyId={facultyId}");
        departmentsPage.EnsureSuccessStatusCode();
        string departmentsHtml = await departmentsPage.Content.ReadAsStringAsync();
        string enterToken = AntiforgeryTokenParser.Parse(departmentsHtml);

        FormUrlEncodedContent enterForm = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = enterToken,
            ["id"] = departmentId.ToString(),
        });
        HttpResponseMessage enterResponse = await client.PostAsync("/SuperAdmin/Departments/Enter", enterForm);
        Assert.Equal(HttpStatusCode.Redirect, enterResponse.StatusCode);

        HttpResponseMessage superAdminHome = await client.GetAsync("/SuperAdmin/Home/Index");
        superAdminHome.EnsureSuccessStatusCode();
        string homeHtml = await superAdminHome.Content.ReadAsStringAsync();
        string setToken = AntiforgeryTokenParser.Parse(homeHtml);

        FormUrlEncodedContent setForm = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = setToken,
            ["mode"] = "3",
            ["returnUrl"] = "/",
        });
        HttpResponseMessage setResponse = await client.PostAsync("/AdminPreview/Set", setForm);
        Assert.Equal(HttpStatusCode.Redirect, setResponse.StatusCode);

        HttpResponseMessage selectUserResponse = await client.GetAsync(setResponse.Headers.Location!);
        selectUserResponse.EnsureSuccessStatusCode();
        string selectUserHtml = await selectUserResponse.Content.ReadAsStringAsync();

        IntegrationTestHtmlAssertions.AssertContainsText(selectUserHtml, "Supervisor One");
        Assert.DoesNotContain(otherEmployee.FullName, selectUserHtml, StringComparison.Ordinal);
    }
}
