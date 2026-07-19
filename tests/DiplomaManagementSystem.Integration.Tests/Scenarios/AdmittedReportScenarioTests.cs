using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class AdmittedReportScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetReport_AfterAdmission_ReturnsAdmittedStudent()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunCheckpointsAndAdmitAsync(services, scenario);

        IAdmittedReportService reportService = services.GetRequiredService<IAdmittedReportService>();
        AdmittedReportDto? report = await reportService.GetReportAsync(scenario.SessionId, CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(scenario.SessionId, report.SessionId);
        Assert.False(string.IsNullOrWhiteSpace(report.SessionLabel));
        AdmittedReportItemDto item = Assert.Single(report.Items);
        Assert.Equal(new DateOnly(2026, 6, 20), item.DefenceDate);
        Assert.False(string.IsNullOrWhiteSpace(item.StudentFullName));
        Assert.False(string.IsNullOrWhiteSpace(item.TopicTitle));
        Assert.False(string.IsNullOrWhiteSpace(item.SupervisorName));
        Assert.Equal(scenario.StudyGroupName, item.StudyGroupName);
        Assert.False(string.IsNullOrWhiteSpace(item.ReviewerName));

        byte[] csv = await reportService.ExportCsvAsync(scenario.SessionId, CancellationToken.None);
        Assert.NotEmpty(csv);
    }
}
