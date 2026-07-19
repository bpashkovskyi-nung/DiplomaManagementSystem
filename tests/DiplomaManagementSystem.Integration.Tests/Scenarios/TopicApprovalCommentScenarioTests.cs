using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class TopicApprovalCommentScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task ApproveTopic_WithOptionalComments_PersistsApprovalComments()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);
        Guid versionId = await WorkflowScenarioRunner.GetPendingTopicVersionIdAsync(
            services,
            scenario.StudentId);

        const string supervisorComment = "Коментар керівника до погодження";
        const string headComment = "Коментар завідувача до затвердження";

        ISupervisorWorkflowService supervisorService = services.GetRequiredService<ISupervisorWorkflowService>();
        IDepartmentHeadWorkflowService headService = services.GetRequiredService<IDepartmentHeadWorkflowService>();

        await supervisorService.ApproveTopicAsync(
            scenario.SupervisorId,
            new ApproveTopicDto(versionId, supervisorComment),
            CancellationToken.None);
        await headService.ApproveTopicAsync(
            scenario.HeadId,
            new ApproveTopicDto(versionId, headComment),
            CancellationToken.None);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        IntegrationScenarioAssertions.AssertTopicApproved(details);

        Assert.Contains(details.History.Comments, comment => comment.Body == supervisorComment);
        Assert.Contains(details.History.Comments, comment => comment.Body == headComment);
    }
}
