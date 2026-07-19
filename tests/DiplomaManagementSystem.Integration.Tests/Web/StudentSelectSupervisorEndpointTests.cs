using System.Net;

using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Web;

[Collection(nameof(IntegrationCollection))]
public sealed class StudentSelectSupervisorEndpointTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task PostSelectSupervisor_RedirectsAndAssignsSupervisor()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using DiplomaManagementSystemWebApplicationFactory factory = fixture.CreateWebFactory();
        HttpClient client = IntegrationTestWebClient.CreateClient(factory, scenario.StudentId);

        HttpResponseMessage indexResponse = await client.GetAsync("/Student/Diploma");
        indexResponse.EnsureSuccessStatusCode();
        string html = await indexResponse.Content.ReadAsStringAsync();
        string token = AntiforgeryTokenParser.Parse(html);

        FormUrlEncodedContent form = new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["DiplomaId"] = scenario.DiplomaId.ToString(),
            ["SupervisorId"] = scenario.SupervisorId.ToString(),
        });

        HttpResponseMessage postResponse = await client.PostAsync("/Student/Diploma/SelectSupervisor", form);

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.Contains("/Student/Diploma", postResponse.Headers.Location?.ToString(), StringComparison.OrdinalIgnoreCase);

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        IStudentDiplomaService studentService = scope.ServiceProvider.GetRequiredService<IStudentDiplomaService>();
        MyDiplomaDto diploma = await studentService.GetMyDiplomaAsync(
            scenario.StudentId,
            CancellationToken.None);

        Assert.Equal(scenario.SupervisorId, diploma.Assignments.SupervisorId);
        Assert.Equal(SupervisorAssignmentStatus.Pending, diploma.Assignments.SupervisorAssignmentStatus);
    }
}
