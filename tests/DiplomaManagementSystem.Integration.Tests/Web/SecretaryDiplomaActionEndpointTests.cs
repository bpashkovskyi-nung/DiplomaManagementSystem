using System.Net;

using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretaryDiplomaActionEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostAssignReviewer_RedirectsAndAssignsReviewer()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToReviewerAssignmentStepAsync(setupScope.ServiceProvider, scenario);

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
        Assert.Contains(
            $"/Secretary/Diplomas/Details/{scenario.DiplomaId}",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        DiplomaDetailsDto? details = await verifyScope.ServiceProvider
            .GetRequiredService<ISecretaryDiplomaDetailsService>()
            .GetDetailsAsync(scenario.SessionId, scenario.DiplomaId, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(scenario.ReviewerId, details.Assignments.ReviewerId);
        Assert.Equal(DiplomaLifecycleStatus.ReviewerAssigned, details.State.LifecycleStatus);
        Assert.Null(details.State.CurrentAdmissionStep);
        Assert.Equal(ReviewAssignmentStatus.Assigned, details.Assignments.ReviewAssignmentStatus);
    }

    [SkippableFact]
    public async Task PostAddComment_RedirectsAndAddsComment()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

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
            ["Body"] = "Коментар секретаря з HTTP-тесту",
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Secretary/Diplomas/AddComment", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        DiplomaDetailsDto? details = await verifyScope.ServiceProvider
            .GetRequiredService<ISecretaryDiplomaDetailsService>()
            .GetDetailsAsync(scenario.SessionId, scenario.DiplomaId, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Contains(
            details.History.Comments,
            comment => comment.Body.Contains("HTTP-тесту", StringComparison.Ordinal));
    }

    [SkippableFact]
    public async Task PostOverrideSupervisor_RedirectsAndChangesSupervisor()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(setupScope.ServiceProvider, scenario);

        IUserProvisioningService userProvisioningService = setupScope.ServiceProvider
            .GetRequiredService<IUserProvisioningService>();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        ApplicationUser replacementSupervisor = await userProvisioningService.CreateEmployeeAsync(
            "Supervisor HTTP",
            $"supervisor.http.{suffix}@test.local");
        await IntegrationDepartmentHelper.AssignEmployeeAsync(
            setupScope.ServiceProvider,
            replacementSupervisor.Id,
            replacementSupervisor.FullName);

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
            ["SupervisorId"] = replacementSupervisor.Id.ToString(),
            ["Reason"] = "Заміна через HTTP-тест",
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Secretary/Diplomas/OverrideSupervisor", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);

        await using AsyncServiceScope verifyScope = factory.Services.CreateAsyncScope();
        DiplomaDetailsDto? details = await verifyScope.ServiceProvider
            .GetRequiredService<ISecretaryDiplomaDetailsService>()
            .GetDetailsAsync(scenario.SessionId, scenario.DiplomaId, CancellationToken.None);

        Assert.NotNull(details);
        Assert.Equal(replacementSupervisor.Id, details.Assignments.SupervisorId);
    }
}
