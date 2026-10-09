using System.Net;

using DiplomaManagementSystem.Application.Admin.StudyGroups.Contracts;
using DiplomaManagementSystem.Application.Admin.StudyGroups.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class AdminStudyGroupsEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetCreate_ShowsFormWithSpecialtyOptions()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync(
            $"/Admin/StudyGroups/Create?defenceSessionId={scenario.SessionId}");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Нова група");
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Спеціальність");
    }

    [SkippableFact]
    public async Task PostCreate_CreatesGroupAndRedirectsToSessionDetails()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];
        string groupName = $"КН-WEB-{suffix}";

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage formPage = await client.GetAsync(
            $"/Admin/StudyGroups/Create?defenceSessionId={scenario.SessionId}");
        formPage.EnsureSuccessStatusCode();
        string formHtml = await formPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(formHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DefenceSessionId"] = scenario.SessionId.ToString(),
            ["Name"] = groupName,
            ["SpecialtyId"] = scenario.SpecialtyId.ToString(),
            ["StudyForm"] = "очної форми навчання",
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Admin/StudyGroups/Create", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            $"/Admin/DefenceSessions/Details/{scenario.SessionId}",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        IStudyGroupAdminService studyGroupAdminService =
            verifyScope.ServiceProvider.GetRequiredService<IStudyGroupAdminService>();
        IReadOnlyList<StudyGroupListItemDto> groups =
            await studyGroupAdminService.GetAllAsync(scenario.SessionId, CancellationToken.None);

        Assert.Contains(groups, group => group.Name == groupName);
    }

    [SkippableFact]
    public async Task PostDelete_RemovesEmptyGroup()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());
        string suffix = Guid.NewGuid().ToString("N")[..6];

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(setupScope.ServiceProvider);
        IStudyGroupAdminService studyGroupAdminService =
            setupScope.ServiceProvider.GetRequiredService<IStudyGroupAdminService>();
        Guid groupId = await studyGroupAdminService.CreateAsync(
            new StudyGroupFormDto(
                null,
                scenario.SessionId,
                $"КН-DEL-{suffix}",
                scenario.SpecialtyId,
                "очної форми навчання"));

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage deletePage = await client.GetAsync($"/Admin/StudyGroups/Delete/{groupId}");
        deletePage.EnsureSuccessStatusCode();
        string deleteHtml = await deletePage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(deleteHtml);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Id"] = groupId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync($"/Admin/StudyGroups/Delete/{groupId}", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        IStudyGroupAdminService verifyService =
            verifyScope.ServiceProvider.GetRequiredService<IStudyGroupAdminService>();
        StudyGroupListItemDto? item = await verifyService.GetListItemAsync(groupId, CancellationToken.None);
        Assert.Null(item);
    }
}
