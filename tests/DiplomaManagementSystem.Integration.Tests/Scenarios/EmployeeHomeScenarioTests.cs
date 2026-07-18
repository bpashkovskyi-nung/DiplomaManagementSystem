using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class EmployeeHomeScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task SupervisorHome_AfterTopicSubmitted_ShowsPendingTopicReview()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(services, scenario);

        IEmployeeHomeService employeeHomeService = services.GetRequiredService<IEmployeeHomeService>();
        EmployeeHomeDto home = await employeeHomeService.GetHomeAsync(scenario.SupervisorId, CancellationToken.None);

        EmployeeRoleCardDto topicReviews = Assert.Single(
            home.Roles,
            role => role.RoleKey == "SupervisorTopicReviews");
        Assert.Equal(1, topicReviews.PendingCount);
        Assert.Equal(EmployeePageTitles.ApproveTopicAsSupervisor, topicReviews.RoleDisplay);
        Assert.Equal("Supervisor", topicReviews.Controller);
        Assert.Equal("TopicReviews", topicReviews.Action);

        EmployeeRoleCardDto studentsCard = Assert.Single(
            home.Roles,
            role => role.RoleKey == "SupervisorStudents");
        Assert.Equal(EmployeePageTitles.MyStudents, studentsCard.RoleDisplay);
        Assert.Equal(1, studentsCard.PendingCount);
    }

    [SkippableFact]
    public async Task SupervisorHome_AtTopicStage_ShowsSupervisorFeedbackCardEvenWhenQueueEmpty()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await WorkflowScenarioRunner.RunUpToTopicSubmittedAsync(scope.ServiceProvider, scenario);

        IEmployeeHomeService employeeHomeService = scope.ServiceProvider.GetRequiredService<IEmployeeHomeService>();
        EmployeeHomeDto home = await employeeHomeService.GetHomeAsync(scenario.SupervisorId, CancellationToken.None);

        EmployeeRoleCardDto feedbackCard = Assert.Single(
            home.Roles,
            role => role.RoleKey == "SupervisorFeedback");
        Assert.Equal(0, feedbackCard.PendingCount);
        Assert.Equal(EmployeePageTitles.SubmitSupervisorFeedback, feedbackCard.RoleDisplay);
        Assert.Equal("Supervisor", feedbackCard.Controller);
        Assert.Equal("Checkpoints", feedbackCard.Action);
    }

    [SkippableFact]
    public async Task DepartmentHeadHome_AfterSupervisorApproval_ShowsPendingTopic()
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

        ISupervisorWorkflowService supervisorService = services.GetRequiredService<ISupervisorWorkflowService>();
        await supervisorService.ApproveTopicAsync(
            scenario.SupervisorId,
            new ApproveTopicDto(versionId, null),
            CancellationToken.None);

        IEmployeeHomeService employeeHomeService = services.GetRequiredService<IEmployeeHomeService>();
        EmployeeHomeDto home = await employeeHomeService.GetHomeAsync(scenario.HeadId, CancellationToken.None);

        EmployeeRoleCardDto headCard = Assert.Single(
            home.Roles,
            role => role.RoleKey == "DepartmentHead");
        Assert.Equal(1, headCard.PendingCount);
        Assert.Equal(EmployeePageTitles.ApproveTopicAsDepartmentHead, headCard.RoleDisplay);
        Assert.Equal("DepartmentHead", headCard.Controller);
        Assert.Equal("PendingTopics", headCard.Action);
    }

    [SkippableFact]
    public async Task FormattingReviewerHome_AfterSupervisorFeedback_ShowsPendingCheckpoint()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(services, scenario);

        IAdmissionReviewService admissionReviewService = services.GetRequiredService<IAdmissionReviewService>();
        await admissionReviewService.CompleteSupervisorFeedbackAsync(
            scenario.SupervisorId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            IntegrationTestDocuments.CreatePdf("supervisor-feedback.pdf"),
            CancellationToken.None);

        IEmployeeHomeService employeeHomeService = services.GetRequiredService<IEmployeeHomeService>();
        EmployeeHomeDto home = await employeeHomeService.GetHomeAsync(scenario.FormattingId, CancellationToken.None);

        EmployeeRoleCardDto formattingCard = Assert.Single(
            home.Roles,
            role => role.RoleKey == "FormattingReview");
        Assert.Equal(1, formattingCard.PendingCount);
    }

    [SkippableFact]
    public async Task ReviewerHome_AfterAssignment_ShowsPendingAssignment()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(services, scenario);

        IAdmissionReviewService admissionReviewService = services.GetRequiredService<IAdmissionReviewService>();

        await admissionReviewService.CompleteSupervisorFeedbackAsync(
            scenario.SupervisorId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            IntegrationTestDocuments.CreatePdf("supervisor-feedback.pdf"),
            CancellationToken.None);
        await admissionReviewService.CompleteFormattingReviewAsync(
            scenario.FormattingId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            CancellationToken.None);
        await admissionReviewService.CompleteAntiPlagiarismAsync(
            scenario.AntiPlagiarismId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            IntegrationTestDocuments.CreatePdf("anti-plagiarism.pdf"),
            CancellationToken.None);

        IEmployeeHomeService employeeHomeService = services.GetRequiredService<IEmployeeHomeService>();
        EmployeeHomeDto home = await employeeHomeService.GetHomeAsync(scenario.ReviewerId, CancellationToken.None);

        EmployeeRoleCardDto reviewerCard = Assert.Single(
            home.Roles,
            role => role.RoleKey == "Reviewer");
        Assert.Equal(1, reviewerCard.PendingCount);
        Assert.Equal(EmployeePageTitles.SubmitExternalReview, reviewerCard.RoleDisplay);
        Assert.Equal("Reviewer", reviewerCard.Controller);

        EmployeeRoleCardDto reviewerStudentsCard = Assert.Single(
            home.Roles,
            role => role.RoleKey == "ReviewerStudents");
        Assert.Equal(EmployeePageTitles.MyReviewStudents, reviewerStudentsCard.RoleDisplay);
        Assert.Equal(1, reviewerStudentsCard.PendingCount);
    }

    [SkippableFact]
    public async Task AntiPlagiarismOfficerHome_AfterFormattingReview_ShowsPendingCheckpoint()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunUpToSupervisorFeedbackStepAsync(services, scenario);

        IAdmissionReviewService admissionReviewService = services.GetRequiredService<IAdmissionReviewService>();
        await admissionReviewService.CompleteSupervisorFeedbackAsync(
            scenario.SupervisorId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            IntegrationTestDocuments.CreatePdf("supervisor-feedback.pdf"),
            CancellationToken.None);
        await admissionReviewService.CompleteFormattingReviewAsync(
            scenario.FormattingId,
            new CompleteCheckpointDto(scenario.DiplomaId, CheckpointOutcome.Approved, null),
            CancellationToken.None);

        IEmployeeHomeService employeeHomeService = services.GetRequiredService<IEmployeeHomeService>();
        EmployeeHomeDto home = await employeeHomeService.GetHomeAsync(scenario.AntiPlagiarismId, CancellationToken.None);

        EmployeeRoleCardDto antiPlagiarismCard = Assert.Single(
            home.Roles,
            role => role.RoleKey == "AntiPlagiarism");
        Assert.Equal(1, antiPlagiarismCard.PendingCount);
        Assert.Equal("AntiPlagiarism", antiPlagiarismCard.Controller);
        Assert.Equal("Pending", antiPlagiarismCard.Action);
    }
}
