using System.Net;

using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AdminDepartmentSelectEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetSelect_WhenMultipleDepartments_ShowsSelectionForm()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        SecondDepartmentSeed second = await IntegrationDepartmentHelper.CreateSecondDepartmentWithAdminAndSessionAsync(
            fixture.CreateProvider(),
            year: 3099);
        await AssignAdminToDepartmentAsync(fixture.CreateProvider(), adminId, second.DepartmentId);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync("/Admin/Department/Select");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "кафедр");
    }

    [SkippableFact]
    public async Task PostSelect_SetsDepartmentAndRedirectsToHome()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        SecondDepartmentSeed second = await IntegrationDepartmentHelper.CreateSecondDepartmentWithAdminAndSessionAsync(
            fixture.CreateProvider(),
            year: 3100);
        await AssignAdminToDepartmentAsync(fixture.CreateProvider(), adminId, second.DepartmentId);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage selectPage = await client.GetAsync("/Admin/Department/Select");
        selectPage.EnsureSuccessStatusCode();
        string selectHtml = await selectPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(selectHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["departmentId"] = second.DepartmentId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Admin/Department/Select", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        string? location = postResponse.Headers.Location?.ToString();
        Assert.NotNull(location);
        Assert.Contains("/Admin", location, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task AssignAdminToDepartmentAsync(
        IServiceProvider serviceProvider,
        Guid userId,
        Guid departmentId)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        bool alreadyAssigned = await dbContext.DepartmentAdminAssignments
            .AnyAsync(assignment => assignment.UserId == userId && assignment.DepartmentId == departmentId);

        if (alreadyAssigned)
        {
            return;
        }

        dbContext.DepartmentAdminAssignments.Add(new DepartmentAdminAssignment
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            UserId = userId,
            AssignedAt = DateTimeOffset.UtcNow,
        });
        await dbContext.SaveChangesAsync();
    }
}
