using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class Feature8ProgressAndDefenceDateScenarioTests(PostgreSqlFixture fixture)
{
    private static readonly DateOnly MilestoneOneDueDate = new(2026, 3, 1);
    private static readonly DateOnly MilestoneTwoDueDate = new(2026, 4, 1);
    private static readonly DateOnly MilestoneThreeDueDate = new(2026, 5, 1);
    private static readonly DateOnly DefenceDateOptionA = new(2026, 6, 20);
    private static readonly DateOnly DefenceDateOptionB = new(2026, 6, 27);

    // TC-INT-F8-001
    [SkippableFact]
    public async Task SecretarySavesMilestonesAndDefenceDates_PersistsAndReturnsInPage()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        ISessionSetupService sessionSetup = services.GetRequiredService<ISessionSetupService>();

        await SaveThreeMilestonesAsync(sessionSetup, scenario.SessionId);
        await sessionSetup.SaveDefenceDatesAsync(
            scenario.SessionId,
            new SaveDefenceDatesDto([DefenceDateOptionA, DefenceDateOptionB]));

        SessionSetupPageDto? page = await sessionSetup.GetPageAsync(scenario.SessionId);

        Assert.NotNull(page);
        Assert.Equal(3, page.Milestones.Count);
        Assert.Equal(MilestoneOneDueDate, page.Milestones[0].DueDate);
        Assert.Equal(30, page.Milestones[0].ExpectedPercent);
        Assert.Equal(MilestoneTwoDueDate, page.Milestones[1].DueDate);
        Assert.Equal(60, page.Milestones[1].ExpectedPercent);
        Assert.Equal(MilestoneThreeDueDate, page.Milestones[2].DueDate);
        Assert.Equal(100, page.Milestones[2].ExpectedPercent);

        Assert.Equal(2, page.AvailableDates.Count);
        Assert.Contains(page.AvailableDates, option => option.Date == DefenceDateOptionA && !option.IsProtected);
        Assert.Contains(page.AvailableDates, option => option.Date == DefenceDateOptionB && !option.IsProtected);
    }

    // TC-INT-F8-002
    [SkippableFact]
    public async Task SupervisorSetsActualPercent_ForOwnStudent_PersistsProgress()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);

        ISessionSetupService sessionSetup = services.GetRequiredService<ISessionSetupService>();
        await SaveThreeMilestonesAsync(sessionSetup, scenario.SessionId);

        SessionSetupPageDto? setupPage = await sessionSetup.GetPageAsync(scenario.SessionId);
        Assert.NotNull(setupPage);
        Guid milestoneId = setupPage.Milestones[0].Id!.Value;

        ISupervisorProgressService supervisorProgress = services.GetRequiredService<ISupervisorProgressService>();
        await supervisorProgress.SetActualPercentAsync(
            scenario.SupervisorId,
            new SetMilestoneProgressDto(scenario.DiplomaId, milestoneId, 45));

        SupervisorProgressPageDto page = await supervisorProgress.GetPageAsync(scenario.SupervisorId, scenario.SessionId);

        SupervisorProgressStudentDto student = Assert.Single(page.Students);
        Assert.Equal(scenario.DiplomaId, student.DiplomaId);
        SupervisorProgressCellDto cell = student.Cells.Single(item => item.MilestoneId == milestoneId);
        Assert.Equal(45, cell.ActualPercent);

        // Updating the same milestone overwrites the previous value instead of duplicating it.
        await supervisorProgress.SetActualPercentAsync(
            scenario.SupervisorId,
            new SetMilestoneProgressDto(scenario.DiplomaId, milestoneId, 55));

        page = await supervisorProgress.GetPageAsync(scenario.SupervisorId, scenario.SessionId);
        cell = page.Students.Single().Cells.Single(item => item.MilestoneId == milestoneId);
        Assert.Equal(55, cell.ActualPercent);
    }

    // TC-INT-F8-003
    [SkippableFact]
    public async Task DepartmentProgressReport_GroupsBySupervisor()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);

        ISessionSetupService sessionSetup = services.GetRequiredService<ISessionSetupService>();
        await SaveThreeMilestonesAsync(sessionSetup, scenario.SessionId);

        SessionSetupPageDto? setupPage = await sessionSetup.GetPageAsync(scenario.SessionId);
        Assert.NotNull(setupPage);
        Guid firstMilestoneId = setupPage.Milestones[0].Id!.Value;

        ISupervisorProgressService supervisorProgress = services.GetRequiredService<ISupervisorProgressService>();
        await supervisorProgress.SetActualPercentAsync(
            scenario.SupervisorId,
            new SetMilestoneProgressDto(scenario.DiplomaId, firstMilestoneId, 40));

        IDepartmentProgressReportService departmentReport = services.GetRequiredService<IDepartmentProgressReportService>();
        DepartmentProgressReportDto? report = await departmentReport.GetReportAsync(scenario.HeadId, scenario.SessionId);

        Assert.NotNull(report);
        Assert.Equal(3, report.Milestones.Count);
        DepartmentProgressSupervisorGroupDto group = Assert.Single(report.Groups);
        Assert.False(string.IsNullOrWhiteSpace(group.SupervisorName));
        Assert.NotEqual("Без керівника", group.SupervisorName);

        DepartmentProgressStudentDto student = Assert.Single(group.Students);
        Assert.Equal(scenario.DiplomaId, student.DiplomaId);
        Assert.Equal(scenario.StudyGroupName, student.StudyGroupName);
        Assert.Equal(40, student.ActualPercents[0]);
        Assert.Null(student.ActualPercents[1]);
        Assert.Null(student.ActualPercents[2]);
    }

    // TC-INT-F8-004
    [SkippableFact]
    public async Task StudentRequestsDefenceDate_AfterAdmit_SecondRequestFails()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        List<DefenceDateOption> options = await AdmitAndSaveDefenceDatesAsync(services, scenario);

        IDefenceDateRequestService defenceDateRequest = services.GetRequiredService<IDefenceDateRequestService>();
        await defenceDateRequest.RequestAsStudentAsync(
            scenario.StudentId,
            new RequestDefenceDateDto(scenario.DiplomaId, options[0].Id));

        DefenceDateRequestFormDto? form = await defenceDateRequest.GetFormForStudentAsync(scenario.StudentId);
        Assert.NotNull(form);
        Assert.Equal(options[0].Date, form.PreferredDate);
        Assert.False(form.CanRequest);

        await Assert.ThrowsAsync<DomainException>(() =>
            defenceDateRequest.RequestAsStudentAsync(
                scenario.StudentId,
                new RequestDefenceDateDto(scenario.DiplomaId, options[1].Id)));
    }

    // TC-INT-F8-005
    [SkippableFact]
    public async Task SupervisorCannotRequestDate_WhenStudentAlreadyRequested()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        List<DefenceDateOption> options = await AdmitAndSaveDefenceDatesAsync(services, scenario);

        IDefenceDateRequestService defenceDateRequest = services.GetRequiredService<IDefenceDateRequestService>();
        await defenceDateRequest.RequestAsStudentAsync(
            scenario.StudentId,
            new RequestDefenceDateDto(scenario.DiplomaId, options[0].Id));

        await Assert.ThrowsAsync<DomainException>(() =>
            defenceDateRequest.RequestAsSupervisorAsync(
                scenario.SupervisorId,
                new RequestDefenceDateDto(scenario.DiplomaId, options[1].Id)));
    }

    // TC-INT-F8-006
    [SkippableFact]
    public async Task SaveDefenceDates_CannotRemoveProtectedDateOption()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        List<DefenceDateOption> options = await AdmitAndSaveDefenceDatesAsync(services, scenario);

        IDefenceDateRequestService defenceDateRequest = services.GetRequiredService<IDefenceDateRequestService>();
        await defenceDateRequest.RequestAsStudentAsync(
            scenario.StudentId,
            new RequestDefenceDateDto(scenario.DiplomaId, options[0].Id));

        ISessionSetupService sessionSetup = services.GetRequiredService<ISessionSetupService>();

        await Assert.ThrowsAsync<DomainException>(() =>
            sessionSetup.SaveDefenceDatesAsync(
                scenario.SessionId,
                new SaveDefenceDatesDto([DefenceDateOptionB])));

        SessionSetupPageDto? page = await sessionSetup.GetPageAsync(scenario.SessionId);
        Assert.NotNull(page);
        Assert.Contains(page.AvailableDates, option => option.Date == DefenceDateOptionA && option.IsProtected);
    }

    // TC-INT-F8-007
    [SkippableFact]
    public async Task ConfirmDefenceDate_DifferentFromPreferred_Succeeds()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        List<DefenceDateOption> options = await AdmitAndSaveDefenceDatesAsync(services, scenario);

        IDefenceDateRequestService defenceDateRequest = services.GetRequiredService<IDefenceDateRequestService>();
        await defenceDateRequest.RequestAsStudentAsync(
            scenario.StudentId,
            new RequestDefenceDateDto(scenario.DiplomaId, options[0].Id));

        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        await secretaryActions.ConfirmDefenceDateAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new ConfirmDefenceDateDto(scenario.DiplomaId, options[1].Date));

        DefenceDateRequestFormDto? form = await defenceDateRequest.GetFormForStudentAsync(scenario.StudentId);

        Assert.NotNull(form);
        Assert.Equal(options[0].Date, form.PreferredDate);
        Assert.Equal(options[1].Date, form.ConfirmedDefenceDate);
        Assert.NotEqual(form.PreferredDate, form.ConfirmedDefenceDate);
    }

    // TC-INT-F8-008
    [SkippableFact]
    public async Task PreferenceQueue_AfterStudentRequest_ShowsRequesterAndPreferredDate()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        List<DefenceDateOption> options = await AdmitAndSaveDefenceDatesAsync(services, scenario);

        IDefenceDateRequestService defenceDateRequest = services.GetRequiredService<IDefenceDateRequestService>();
        await defenceDateRequest.RequestAsStudentAsync(
            scenario.StudentId,
            new RequestDefenceDateDto(scenario.DiplomaId, options[0].Id));

        IDefenceDatePreferenceQueueService queueService =
            services.GetRequiredService<IDefenceDatePreferenceQueueService>();
        DefenceDatePreferenceQueueDto? queue = await queueService.GetQueueAsync(scenario.SessionId);

        Assert.NotNull(queue);
        Assert.Contains(DefenceDateOptionA, queue.AvailableDates);
        Assert.Contains(DefenceDateOptionB, queue.AvailableDates);

        DefenceDatePreferenceItemDto item = Assert.Single(queue.Items);
        Assert.Equal(scenario.DiplomaId, item.DiplomaId);
        Assert.Equal(options[0].Date, item.PreferredDate);
        Assert.Equal("Студент", item.RequesterTypeLabel);
        Assert.Null(item.ConfirmedDefenceDate);
        Assert.False(string.IsNullOrWhiteSpace(item.StudentFullName));
        Assert.False(string.IsNullOrWhiteSpace(item.RequesterName));
    }

    private static async Task SaveThreeMilestonesAsync(ISessionSetupService sessionSetup, Guid sessionId) =>
        await sessionSetup.SaveMilestonesAsync(
            sessionId,
            new SaveMilestonesDto(
            [
                new SaveMilestoneItemDto(MilestoneOneDueDate, 30),
                new SaveMilestoneItemDto(MilestoneTwoDueDate, 60),
                new SaveMilestoneItemDto(MilestoneThreeDueDate, 100),
            ]));

    private static async Task<List<DefenceDateOption>> AdmitAndSaveDefenceDatesAsync(
        IServiceProvider services,
        IntegrationScenario scenario)
    {
        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(services, scenario);
        await WorkflowScenarioRunner.RunUpToReadyForAdmissionAsync(services, scenario);

        ISecretaryDiplomaActionService secretaryActions = services.GetRequiredService<ISecretaryDiplomaActionService>();
        await secretaryActions.AdmitAsync(
            scenario.SecretaryId,
            scenario.SessionId,
            new AdmitDiplomaDto(scenario.DiplomaId));

        ISessionSetupService sessionSetup = services.GetRequiredService<ISessionSetupService>();
        await sessionSetup.SaveDefenceDatesAsync(
            scenario.SessionId,
            new SaveDefenceDatesDto([DefenceDateOptionA, DefenceDateOptionB]));

        IApplicationDbContext dbContext = services.GetRequiredService<IApplicationDbContext>();
        return await dbContext.DefenceDateOptions
            .AsNoTracking()
            .Where(option => option.DefenceSessionId == scenario.SessionId)
            .OrderBy(option => option.Date)
            .ToListAsync();
    }
}
