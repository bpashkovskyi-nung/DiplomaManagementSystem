using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class DiplomaWorkflowInvariantScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task SaveChanges_ApprovedTopicWithoutSupervisor_ThrowsDomainException()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        Diploma diploma = await dbContext.Diplomas.FindAsync(scenario.DiplomaId)
                          ?? throw new InvalidOperationException("Diploma not found.");

        DiplomaTopicVersion topicVersion = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diploma.Id,
            VersionNumber = 1,
            Title = "Invalid seeded topic",
            Status = TopicVersionStatus.Approved,
            SubmittedAt = DateTimeOffset.UtcNow,
            ReviewedAt = DateTimeOffset.UtcNow,
            ReviewedById = scenario.HeadId,
        };

        diploma.SupervisorId = null;
        diploma.SupervisorAssignmentStatus = SupervisorAssignmentStatus.Pending;
        diploma.LifecycleStatus = DiplomaLifecycleStatus.ReviewerAssigned;
        dbContext.DiplomaTopicVersions.Add(topicVersion);

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            dbContext.SaveChangesAsync(CancellationToken.None));

        Assert.Contains("confirmed supervisor", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task WorkflowScenario_ApproveTopicWithConfirmedSupervisor_SavesSuccessfully()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);
        Guid versionId = await WorkflowScenarioRunner.GetPendingTopicVersionIdAsync(services, scenario.StudentId);

        ISupervisorWorkflowService supervisorService = services.GetRequiredService<ISupervisorWorkflowService>();
        IDepartmentHeadWorkflowService headService = services.GetRequiredService<IDepartmentHeadWorkflowService>();

        await supervisorService.ApproveTopicAsync(
            scenario.SupervisorId,
            new ApproveTopicDto(versionId, null),
            CancellationToken.None);
        await headService.ApproveTopicAsync(
            scenario.HeadId,
            new ApproveTopicDto(versionId, null),
            CancellationToken.None);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        IntegrationScenarioAssertions.AssertTopicApproved(details);
        Assert.Equal(scenario.SupervisorId, details.Assignments.SupervisorId);
    }
}
