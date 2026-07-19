using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class ReviewerStudentsEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task GetStudents_AfterReviewerAssigned_ShowsStudentInList()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope setupScope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunTopicApprovalAsync(setupScope.ServiceProvider, scenario);

        ISecretaryDiplomaActionService secretaryActions =
            setupScope.ServiceProvider.GetRequiredService<ISecretaryDiplomaActionService>();
        await secretaryActions.AssignReviewerAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new AssignReviewerDto(scenario.DiplomaId, scenario.ReviewerId),
            CancellationToken.None);

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.ReviewerId);

        HttpResponseMessage response = await client.GetAsync("/Employee/Reviewer/Students");

        response.EnsureSuccessStatusCode();
        string html = await response.Content.ReadAsStringAsync();
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Student One");
        IntegrationTestHtmlAssertions.AssertContainsText(html, EmployeePageTitles.MyReviewStudents);
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Керівник");
        IntegrationTestHtmlAssertions.AssertContainsText(html, "Supervisor One");
    }
}
