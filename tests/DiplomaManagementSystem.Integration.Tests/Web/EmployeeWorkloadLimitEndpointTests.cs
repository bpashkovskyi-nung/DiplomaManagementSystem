using System.Net;

using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Contracts;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class EmployeeWorkloadLimitEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetWorkloadLimits_AsAdmin_ShowsPage()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();
        Guid adminId = await IntegrationAdminHelper.CreateAdminUserAsync(fixture.CreateProvider());

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        IEmployeeWorkloadLimitAdminService limitAdmin =
            setupScope.ServiceProvider.GetRequiredService<IEmployeeWorkloadLimitAdminService>();
        await limitAdmin.SetLimitAsync(
            new SetEmployeeWorkloadLimitDto(scenario.SessionId, scenario.SupervisorId, 3, 2),
            CancellationToken.None);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, adminId);

        HttpResponseMessage response = await client.GetAsync(
            $"/Admin/EmployeeWorkloadLimits?defenceSessionId={scenario.SessionId}");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Ліміти викладачів");
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Supervisor One");
    }

    [SkippableFact]
    public async Task PostAssignReviewer_WhenReviewerLimitReached_ShowsError()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToReviewerAssignmentStepAsync(setupScope.ServiceProvider, scenario);

        IEmployeeWorkloadLimitAdminService limitAdmin =
            setupScope.ServiceProvider.GetRequiredService<IEmployeeWorkloadLimitAdminService>();
        await limitAdmin.SetLimitAsync(
            new SetEmployeeWorkloadLimitDto(scenario.SessionId, scenario.ReviewerId, null, 0),
            CancellationToken.None);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage detailsPage = await client.GetAsync($"/Secretary/Diplomas/Details/{scenario.DiplomaId}");
        detailsPage.EnsureSuccessStatusCode();
        string html = await detailsPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DiplomaId"] = scenario.DiplomaId.ToString(),
            ["ReviewerId"] = scenario.ReviewerId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Secretary/Diplomas/AssignReviewer", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        DiplomaDetailsDto? details = await verifyScope.ServiceProvider
            .GetRequiredService<ISecretaryDiplomaDetailsService>()
            .GetDetailsAsync(scenario.SessionId, scenario.DiplomaId, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Null(details.Assignments.ReviewerId);

        HttpResponseMessage redirectedPage = await client.GetAsync(postResponse.Headers.Location!);
        redirectedPage.EnsureSuccessStatusCode();
        string redirectedHtml = await redirectedPage.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(redirectedHtml, EmployeeWorkloadLimitMessages.ReviewerLimitReached);
    }
}
