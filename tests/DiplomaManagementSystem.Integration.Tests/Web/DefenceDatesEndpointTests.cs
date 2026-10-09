using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class DefenceDatesEndpointTests(PostgreSqlFixture fixture)
{
    // TC-INT-F8-HTTP-004
    [SkippableFact]
    public async Task GetPreferenceQueue_AfterStudentRequest_ShowsPreferredDate()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = setupScope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(services, scenario);
        await WorkflowScenarioRunner.RunUpToReadyForAdmissionAsync(services, scenario);

        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        await secretaryActions.AdmitAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new AdmitDiplomaDto(scenario.DiplomaId));

        DateOnly preferred = new(2026, 6, 20);
        ISessionSetupService sessionSetup = services.GetRequiredService<ISessionSetupService>();
        await sessionSetup.SaveDefenceDatesAsync(
            scenario.SessionId,
            new SaveDefenceDatesDto([preferred, new DateOnly(2026, 6, 27)]));

        DefenceDateOption option = await services.GetRequiredService<IApplicationDbContext>().DefenceDateOptions
            .AsNoTracking()
            .Where(item => item.DefenceSessionId == scenario.SessionId && item.Date == preferred)
            .SingleAsync();

        await services.GetRequiredService<IDefenceDateRequestService>().RequestAsStudentAsync(
            scenario.StudentId,
            new RequestDefenceDateDto(scenario.DiplomaId, option.Id));

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.SecretaryId);
        IntegrationTestWebClient.SetSecretarySessionCookie(client, scenario.SessionId);

        HttpResponseMessage response = await client.GetAsync("/Secretary/DefenceDates/Index");
        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();

        Assert.Contains("20.06.2026", html, StringComparison.Ordinal);
        Assert.Contains("Студент", html, StringComparison.Ordinal);
    }
}
