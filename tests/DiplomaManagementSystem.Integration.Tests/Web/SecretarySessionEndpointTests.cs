using System.Net;

using DiplomaManagementSystem.Application.Admin.AnnualRoles.Contracts;
using DiplomaManagementSystem.Application.Admin.AnnualRoles.Dtos;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class SecretarySessionEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostSelect_RedirectsToDashboardAndSetsSession()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = setupScope.ServiceProvider;
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(services);

        IDefenceSessionService defenceSessionService = services.GetRequiredService<IDefenceSessionService>();
        IAnnualRoleService annualRoleService = services.GetRequiredService<IAnnualRoleService>();

        Guid secondSessionId = await defenceSessionService.CreateAsync(
            new DefenceSessionFormDto(null, 2025, DefenceSessionType.Master, 2));
        await annualRoleService.AssignAsync(
            new AssignAnnualRoleDto(secondSessionId, AnnualRoleType.ExamCommissionSecretary, scenario.SecretaryId));

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);

        HttpResponseMessage selectPage = await client.GetAsync("/Secretary/Session/Select");
        selectPage.EnsureSuccessStatusCode();
        string html = await selectPage.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["sessionId"] = secondSessionId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Secretary/Session/Select", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains(
            "/Secretary/Dashboard",
            postResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage verifyResponse = await client.GetAsync("/Secretary/Session/Select");
        Assert.Equal(HttpStatusCode.Redirect, verifyResponse.StatusCode);
        Assert.Contains(
            "/Secretary/Dashboard",
            verifyResponse.Headers.Location?.ToString(),
            StringComparison.OrdinalIgnoreCase);
    }
}
