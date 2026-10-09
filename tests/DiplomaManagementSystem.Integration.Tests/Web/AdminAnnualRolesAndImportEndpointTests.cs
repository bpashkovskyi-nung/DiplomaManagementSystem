using System.Net;
using System.Net.Http.Headers;
using System.Text;

using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AdminAnnualRolesEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetIndex_ShowsAnnualRolesPage()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync(
            $"/Admin/AnnualRoles?defenceSessionId={scenario.SessionId}");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Завідувач");
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Секретар");
    }

    [SkippableFact]
    public async Task PostAssign_UpdatesRoleAssignment()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage page = await client.GetAsync(
            $"/Admin/AnnualRoles?defenceSessionId={scenario.SessionId}");
        page.EnsureSuccessStatusCode();
        string html = await page.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DefenceSessionId"] = scenario.SessionId.ToString(),
            ["RoleType"] = "DepartmentHead",
            ["EmployeeId"] = scenario.ReviewerId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Admin/AnnualRoles/Assign", form);

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        string resultHtml = await postResponse.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(resultHtml, "Роль призначено");
        IntegrationTestHtmlAssertions.AssertContainsText(resultHtml, "Reviewer One");
    }

    [SkippableFact]
    public async Task PostSaveCommission_PersistsExternalRoster()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage page = await client.GetAsync(
            $"/Admin/AnnualRoles?defenceSessionId={scenario.SessionId}");
        page.EnsureSuccessStatusCode();
        string html = await page.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Екзаменаційна комісія");
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DefenceSessionId"] = scenario.SessionId.ToString(),
            ["Chair.IsExternal"] = "true",
            ["Chair.FullName"] = "Голова Зовнішній",
            ["Chair.Position"] = "д.т.н., професор",
            ["Members[0].IsExternal"] = "true",
            ["Members[0].FullName"] = "Член Перший",
            ["Members[0].Position"] = "к.т.н.",
            ["Members[1].IsExternal"] = "true",
            ["Members[1].FullName"] = "Член Другий",
            ["Members[1].Position"] = "доцент",
            ["Members[2].IsExternal"] = "true",
            ["Members[2].FullName"] = "Член Третій",
            ["Members[2].Position"] = "старший викладач",
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Admin/AnnualRoles/SaveCommission", form);

        Assert.Equal(HttpStatusCode.OK, postResponse.StatusCode);
        string resultHtml = await postResponse.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(resultHtml, "Склад ЕК збережено");
        IntegrationTestHtmlAssertions.AssertContainsText(resultHtml, "Голова Зовнішній");
        IntegrationTestHtmlAssertions.AssertContainsText(resultHtml, "Член Третій");

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        int count = await dbContext.ExaminationCommissionParticipants.CountAsync(
            participant => participant.DefenceSessionId == scenario.SessionId);
        Assert.Equal(4, count);
    }
}

[Collection(nameof(IntegrationCollection))]
public sealed class AdminHomeAndDepartmentEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetAdminHome_ShowsPage()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync("/Admin/Home");

        response.EnsureSuccessStatusCode();
    }

    [SkippableFact]
    public async Task GetDepartmentSelect_WhenSingleDepartment_RedirectsToHome()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync("/Admin/Department/Select");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        string? location = response.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/Admin", location, StringComparison.OrdinalIgnoreCase);
    }
}

[Collection(nameof(IntegrationCollection))]
public sealed class SuperAdminOrganizationImportEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetIndex_ShowsImportForm()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage response = await client.GetAsync("/SuperAdmin/OrganizationImport");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "JSON");
    }

    [SkippableFact]
    public async Task PostImport_CreatesFacultiesAndDepartments()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string facultyName = $"Факультет Import {suffix}";
        string departmentName = $"Кафедра Import {suffix}";
        string json = $$"""
            [
              {
                "name": "{{facultyName}}",
                "departments": [
                  {
                    "name": "{{departmentName}}"
                  }
                ]
              }
            ]
            """;

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage formPage = await client.GetAsync("/SuperAdmin/OrganizationImport");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        using MultipartFormDataContent form = new();
        form.Add(new StringContent(token), "__RequestVerificationToken");
        form.Add(new StringContent("CreateOnly"), "Mode");
        ByteArrayContent fileContent = new(Encoding.UTF8.GetBytes(json));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        form.Add(fileContent, "File", $"org-{suffix}.json");

        HttpResponseMessage postResponse = await client.PostAsync("/SuperAdmin/OrganizationImport", form);

        postResponse.EnsureSuccessStatusCode();
        string resultHtml = await postResponse.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(resultHtml, "факультетів створено");

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        IApplicationDbContext dbContext = verifyScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        bool facultyExists = await dbContext.Faculties
            .AsNoTracking()
            .AnyAsync(faculty => faculty.Name == facultyName && faculty.IsActive);
        bool departmentExists = await dbContext.Departments
            .AsNoTracking()
            .AnyAsync(department => department.Name == departmentName && department.IsActive);

        Assert.True(facultyExists);
        Assert.True(departmentExists);
    }
}
