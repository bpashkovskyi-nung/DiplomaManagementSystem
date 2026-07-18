using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Contracts;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class EmployeeWorkloadLimitScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task ConfirmSupervisor_WhenLimitReached_Throws()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await SetSupervisorLimitAsync(services, scenario, maxSupervisorStudents: 0);

        IStudentDiplomaService studentDiplomaService = services.GetRequiredService<IStudentDiplomaService>();
        await studentDiplomaService.SelectSupervisorAsync(
            scenario.StudentId,
            new SelectSupervisorDto(scenario.DiplomaId, scenario.SupervisorId),
            CancellationToken.None);

        ISupervisorWorkflowService supervisorWorkflow =
            services.GetRequiredService<ISupervisorWorkflowService>();

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            supervisorWorkflow.ConfirmStudentAsync(scenario.SupervisorId, scenario.DiplomaId, CancellationToken.None));

        Assert.Equal(EmployeeWorkloadLimitMessages.SupervisorLimitReached, exception.Message);
    }

    [SkippableFact]
    public async Task ConfirmSupervisor_WhenBelowLimit_Succeeds()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await SetSupervisorLimitAsync(services, scenario, maxSupervisorStudents: 1);

        IStudentDiplomaService studentDiplomaService = services.GetRequiredService<IStudentDiplomaService>();
        await studentDiplomaService.SelectSupervisorAsync(
            scenario.StudentId,
            new SelectSupervisorDto(scenario.DiplomaId, scenario.SupervisorId),
            CancellationToken.None);

        ISupervisorWorkflowService supervisorWorkflow =
            services.GetRequiredService<ISupervisorWorkflowService>();
        await supervisorWorkflow.ConfirmStudentAsync(scenario.SupervisorId, scenario.DiplomaId, CancellationToken.None);

        MyDiplomaDto diploma = await studentDiplomaService.GetMyDiplomaAsync(scenario.StudentId, CancellationToken.None);
        Assert.Equal(SupervisorAssignmentStatus.Confirmed, diploma.Assignments.SupervisorAssignmentStatus);
    }

    [SkippableFact]
    public async Task SelectSupervisor_WhenLimitReached_StillAllowsRequest()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await SetSupervisorLimitAsync(services, scenario, maxSupervisorStudents: 0);

        IStudentDiplomaService studentDiplomaService = services.GetRequiredService<IStudentDiplomaService>();
        await studentDiplomaService.SelectSupervisorAsync(
            scenario.StudentId,
            new SelectSupervisorDto(scenario.DiplomaId, scenario.SupervisorId),
            CancellationToken.None);

        MyDiplomaDto diploma = await studentDiplomaService.GetMyDiplomaAsync(scenario.StudentId, CancellationToken.None);
        Assert.Equal(SupervisorAssignmentStatus.Pending, diploma.Assignments.SupervisorAssignmentStatus);
        Assert.Equal(scenario.SupervisorId, diploma.Assignments.SupervisorId);
    }

    [SkippableFact]
    public async Task OverrideSupervisor_WhenLimitReached_Throws()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);

        ApplicationUser replacementSupervisor = await CreateEmployeeAsync(services, "Supervisor Replacement");
        await SetSupervisorLimitAsync(services, scenario, maxSupervisorStudents: 0, employeeId: replacementSupervisor.Id);

        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            secretaryActions.OverrideSupervisorAsync(
                scenario.SecretaryId,
                scenario.SessionId,
                new OverrideSupervisorDto(scenario.DiplomaId, replacementSupervisor.Id, "Заміна"),
                CancellationToken.None));

        Assert.Equal(EmployeeWorkloadLimitMessages.SupervisorLimitReached, exception.Message);
    }

    [SkippableFact]
    public async Task OverrideSupervisor_WhenBelowLimit_Succeeds()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);

        ApplicationUser replacementSupervisor = await CreateEmployeeAsync(services, "Supervisor Replacement");
        await SetSupervisorLimitAsync(services, scenario, maxSupervisorStudents: 1, employeeId: replacementSupervisor.Id);

        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        await secretaryActions.OverrideSupervisorAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new OverrideSupervisorDto(scenario.DiplomaId, replacementSupervisor.Id, "Заміна"),
            CancellationToken.None);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        Assert.Equal(replacementSupervisor.Id, details.Assignments.SupervisorId);
        Assert.Equal(SupervisorAssignmentStatus.Confirmed, details.Assignments.SupervisorAssignmentStatus);
    }

    [SkippableFact]
    public async Task AssignReviewer_WhenLimitReached_Throws()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToReviewerAssignmentStepAsync(services, scenario);
        await SetReviewerLimitAsync(services, scenario, maxReviewerStudents: 0);

        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            secretaryActions.AssignReviewerAsync(
                scenario.SecretaryId,
                scenario.SessionId,
                new AssignReviewerDto(scenario.DiplomaId, scenario.ReviewerId),
                CancellationToken.None));

        Assert.Equal(EmployeeWorkloadLimitMessages.ReviewerLimitReached, exception.Message);
    }

    [SkippableFact]
    public async Task AssignReviewer_WhenBelowLimit_Succeeds()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToReviewerAssignmentStepAsync(services, scenario);
        await SetReviewerLimitAsync(services, scenario, maxReviewerStudents: 1);

        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        await secretaryActions.AssignReviewerAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new AssignReviewerDto(scenario.DiplomaId, scenario.ReviewerId),
            CancellationToken.None);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        Assert.Equal(scenario.ReviewerId, details.Assignments.ReviewerId);
        Assert.Equal(ReviewAssignmentStatus.Assigned, details.Assignments.ReviewAssignmentStatus);
    }

    [SkippableFact]
    public async Task Admin_SetAndClearLimits_UpdatesPage()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IEmployeeWorkloadLimitAdminService limitAdmin =
            scope.ServiceProvider.GetRequiredService<IEmployeeWorkloadLimitAdminService>();

        await limitAdmin.SetLimitAsync(
            new SetEmployeeWorkloadLimitDto(scenario.SessionId, scenario.SupervisorId, 7, 4),
            CancellationToken.None);

        EmployeeWorkloadLimitsPageDto page = await limitAdmin.GetPageAsync(scenario.SessionId, CancellationToken.None)
            ?? throw new InvalidOperationException("Page not found.");

        EmployeeWorkloadLimitRowDto row = Assert.Single(page.Rows, item => item.EmployeeId == scenario.SupervisorId);
        Assert.Equal(7, row.MaxSupervisorStudents);
        Assert.Equal(4, row.MaxReviewerStudents);

        await limitAdmin.SetLimitAsync(
            new SetEmployeeWorkloadLimitDto(scenario.SessionId, scenario.SupervisorId, null, null),
            CancellationToken.None);

        page = await limitAdmin.GetPageAsync(scenario.SessionId, CancellationToken.None)
            ?? throw new InvalidOperationException("Page not found.");
        row = Assert.Single(page.Rows, item => item.EmployeeId == scenario.SupervisorId);
        Assert.Null(row.MaxSupervisorStudents);
        Assert.Null(row.MaxReviewerStudents);
    }

    private static async Task SetSupervisorLimitAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        int maxSupervisorStudents,
        Guid? employeeId = null)
    {
        IEmployeeWorkloadLimitAdminService limitAdmin = services.GetRequiredService<IEmployeeWorkloadLimitAdminService>();
        await limitAdmin.SetLimitAsync(
            new SetEmployeeWorkloadLimitDto(
                scenario.SessionId,
                employeeId ?? scenario.SupervisorId,
                maxSupervisorStudents,
                null),
            CancellationToken.None);
    }

    private static async Task SetReviewerLimitAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        int maxReviewerStudents)
    {
        IEmployeeWorkloadLimitAdminService limitAdmin = services.GetRequiredService<IEmployeeWorkloadLimitAdminService>();
        await limitAdmin.SetLimitAsync(
            new SetEmployeeWorkloadLimitDto(scenario.SessionId, scenario.ReviewerId, null, maxReviewerStudents),
            CancellationToken.None);
    }

    private static async Task<ApplicationUser> CreateEmployeeAsync(IServiceProvider services, string fullName)
    {
        IUserProvisioningService userProvisioningService = services.GetRequiredService<IUserProvisioningService>();
        string suffix = Guid.NewGuid().ToString("N")[..8];
        ApplicationUser employee = await userProvisioningService.CreateEmployeeAsync(fullName, $"{suffix}@test.local");
        await IntegrationDepartmentHelper.AssignEmployeeAsync(services, employee.Id, employee.FullName);
        return employee;
    }
}
