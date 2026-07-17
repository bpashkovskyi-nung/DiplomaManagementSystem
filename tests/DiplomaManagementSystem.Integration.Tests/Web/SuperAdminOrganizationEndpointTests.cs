using System.Net;

using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SuperAdminOrganizationEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetFaculties_ShowsList()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage response = await client.GetAsync("/SuperAdmin/Faculties");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Факультет");
    }

    [SkippableFact]
    public async Task GetDepartments_WithFacultyId_ShowsDepartments()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        Guid facultyId = await IntegrationDepartmentHelper.GetDefaultFacultyIdAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage response = await client.GetAsync($"/SuperAdmin/Departments?facultyId={facultyId}");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Кафедр");
    }

    [SkippableFact]
    public async Task PostAddSpecialty_AddsSpecialtyToDepartment()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        Guid departmentId = await IntegrationDepartmentHelper.GetDefaultDepartmentIdAsync(fixture.CreateProvider());
        Guid facultyId = await IntegrationDepartmentHelper.GetDefaultFacultyIdAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string code = $"7{suffix[..3]}";
        string name = $"Спеціальність {suffix}";

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage editPage = await client.GetAsync($"/SuperAdmin/Departments/Edit/{departmentId}");
        editPage.EnsureSuccessStatusCode();
        string editHtml = await editPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(editHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["departmentId"] = departmentId.ToString(),
            ["facultyId"] = facultyId.ToString(),
            ["code"] = code,
            ["name"] = name,
        });

        HttpResponseMessage postResponse = await client.PostAsync("/SuperAdmin/Departments/AddSpecialty", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            $"/SuperAdmin/Departments/Edit/{departmentId}",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        IApplicationDbContext dbContext = verifyScope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        bool exists = await dbContext.Specialties
            .AsNoTracking()
            .AnyAsync(
                specialty => specialty.DepartmentId == departmentId && specialty.Code == code && specialty.IsActive,
                CancellationToken.None);

        Assert.True(exists);
    }

    [SkippableFact]
    public async Task PostCreateFaculty_AddsFacultyAndShowsOnIndex()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string facultyName = $"Факультет Web {suffix}";

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage formPage = await client.GetAsync("/SuperAdmin/Faculties/Create");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Name"] = facultyName,
        });

        HttpResponseMessage postResponse = await client.PostAsync("/SuperAdmin/Faculties/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/SuperAdmin/Faculties",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage indexPage = await client.GetAsync("/SuperAdmin/Faculties");
        indexPage.EnsureSuccessStatusCode();
        string indexHtml = await indexPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(indexHtml, facultyName);
    }

    [SkippableFact]
    public async Task PostEditFaculty_UpdatesFacultyName()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string facultyName = $"Факультет Edit {suffix}";
        string updatedName = $"Факультет Updated {suffix}";

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage createPage = await client.GetAsync("/SuperAdmin/Faculties/Create");
        createPage.EnsureSuccessStatusCode();
        string createHtml = await createPage.Content.ReadAsStringAsync();
        string createToken = AntiforgeryTokenParser.Parse(createHtml);

        FormUrlEncodedContent createForm = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = createToken,
            ["Name"] = facultyName,
        });

        HttpResponseMessage createResponse = await client.PostAsync("/SuperAdmin/Faculties/Create", createForm);
        Assert.Equal(HttpStatusCode.Redirect, createResponse.StatusCode);

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        Guid facultyId = await dbContext.Faculties
            .AsNoTracking()
            .Where(faculty => faculty.Name == facultyName)
            .Select(faculty => faculty.Id)
            .SingleAsync();

        HttpResponseMessage formPage = await client.GetAsync($"/SuperAdmin/Faculties/Edit/{facultyId}");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = facultyId.ToString(),
            ["Name"] = updatedName,
        });

        HttpResponseMessage postResponse = await client.PostAsync($"/SuperAdmin/Faculties/Edit/{facultyId}", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        HttpResponseMessage indexPage = await client.GetAsync("/SuperAdmin/Faculties");
        indexPage.EnsureSuccessStatusCode();
        string indexHtml = await indexPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(indexHtml, updatedName);
    }

    [SkippableFact]
    public async Task PostAssignAdmin_AssignsDepartmentEmployeeAsAdmin()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        Guid departmentId = await IntegrationDepartmentHelper.GetDefaultDepartmentIdAsync(fixture.CreateProvider());
        Guid facultyId = await IntegrationDepartmentHelper.GetDefaultFacultyIdAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage editPage = await client.GetAsync($"/SuperAdmin/Departments/Edit/{departmentId}");
        editPage.EnsureSuccessStatusCode();
        string editHtml = await editPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(editHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["departmentId"] = departmentId.ToString(),
            ["facultyId"] = facultyId.ToString(),
            ["assignUserId"] = scenario.SupervisorId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/SuperAdmin/Departments/AssignAdmin", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            $"/SuperAdmin/Departments/Edit/{departmentId}",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage redirectedPage = await client.GetAsync(postResponse.Headers.Location!);
        redirectedPage.EnsureSuccessStatusCode();
        string redirectedHtml = await redirectedPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(redirectedHtml, "Supervisor One");
    }

    [SkippableFact]
    public async Task GetCreateDepartment_ShowsForm()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        Guid facultyId = await IntegrationDepartmentHelper.GetDefaultFacultyIdAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage formPage = await client.GetAsync($"/SuperAdmin/Departments/Create?facultyId={facultyId}");

        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(formHtml, "Нова кафедра");
    }

    [SkippableFact]
    public async Task PostCreateDepartment_AddsDepartmentToFaculty()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        Guid facultyId = await IntegrationDepartmentHelper.GetDefaultFacultyIdAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string departmentName = $"Кафедра Web Create {suffix}";

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage formPage = await client.GetAsync($"/SuperAdmin/Departments/Create?facultyId={facultyId}");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["FacultyId"] = facultyId.ToString(),
            ["Name"] = departmentName,
        });

        HttpResponseMessage postResponse = await client.PostAsync("/SuperAdmin/Departments/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        HttpResponseMessage listPage = await client.GetAsync($"/SuperAdmin/Departments?facultyId={facultyId}");
        listPage.EnsureSuccessStatusCode();
        string listHtml = await listPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(listHtml, departmentName);
    }

    [SkippableFact]
    public async Task PostDeactivateSpecialty_WhenNoGroups_DeactivatesSpecialty()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid superAdminId = await IntegrationSuperAdminHelper.CreateSuperAdminUserAsync(fixture.CreateProvider());
        Guid departmentId = await IntegrationDepartmentHelper.GetDefaultDepartmentIdAsync(fixture.CreateProvider());
        Guid facultyId = await IntegrationDepartmentHelper.GetDefaultFacultyIdAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string code = $"8{suffix[..3]}";

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, superAdminId);

        HttpResponseMessage editPage = await client.GetAsync($"/SuperAdmin/Departments/Edit/{departmentId}");
        editPage.EnsureSuccessStatusCode();
        string editHtml = await editPage.Content.ReadAsStringAsync();
        string addToken = AntiforgeryTokenParser.Parse(editHtml);

        FormUrlEncodedContent addForm = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = addToken,
            ["departmentId"] = departmentId.ToString(),
            ["facultyId"] = facultyId.ToString(),
            ["code"] = code,
            ["name"] = $"Тимчасова {suffix}",
        });
        HttpResponseMessage addResponse = await client.PostAsync("/SuperAdmin/Departments/AddSpecialty", addForm);
        Assert.Equal(HttpStatusCode.Redirect, addResponse.StatusCode);

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        Guid specialtyId = await dbContext.Specialties
            .AsNoTracking()
            .Where(specialty => specialty.DepartmentId == departmentId && specialty.Code == code)
            .Select(specialty => specialty.Id)
            .FirstAsync();

        HttpResponseMessage deactivatePage = await client.GetAsync($"/SuperAdmin/Departments/Edit/{departmentId}");
        deactivatePage.EnsureSuccessStatusCode();
        string deactivateHtml = await deactivatePage.Content.ReadAsStringAsync();
        string deactivateToken = AntiforgeryTokenParser.Parse(deactivateHtml);

        FormUrlEncodedContent deactivateForm = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = deactivateToken,
            ["specialtyId"] = specialtyId.ToString(),
            ["departmentId"] = departmentId.ToString(),
        });

        HttpResponseMessage deactivateResponse = await client.PostAsync(
            "/SuperAdmin/Departments/DeactivateSpecialty",
            deactivateForm);

        Assert.Equal(HttpStatusCode.Redirect, deactivateResponse.StatusCode);

        bool isActive = await dbContext.Specialties
            .AsNoTracking()
            .Where(specialty => specialty.Id == specialtyId)
            .Select(specialty => specialty.IsActive)
            .FirstAsync();
        Assert.False(isActive);
    }
}
