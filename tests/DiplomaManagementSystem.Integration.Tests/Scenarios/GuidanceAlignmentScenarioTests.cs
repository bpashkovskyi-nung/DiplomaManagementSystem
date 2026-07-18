using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class GuidanceAlignmentScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task AfterTopicApproval_DeclareWorkReadyBlockedReasonMatchesGuidance()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunTopicApprovalAsync(services, scenario);

        MyDiplomaDto diploma = await IntegrationScenarioAssertions.GetStudentDiplomaAsync(
            services,
            scenario.StudentId);

        Assert.Equal(DiplomaLifecycleStatus.TopicApproved, diploma.State!.LifecycleStatus);
        Assert.NotNull(diploma.Actions);
        Assert.False(diploma.Actions.CanDeclareWorkReady);
        Assert.False(diploma.Actions.CanUploadWork);

        string? expected = DiplomaWorkflowGuidance.BuildDeclareWorkReadyBlockedReason(
            showSection: true,
            sessionActive: true,
            lifecycleStatus: DiplomaLifecycleStatus.TopicApproved,
            hasStudentWork: false);

        Assert.Equal(expected, diploma.Actions.DeclareWorkReadyBlockedReason);
        Assert.Contains("рецензента", diploma.Actions.DeclareWorkReadyBlockedReason, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task BeforeTopicApproval_SecretaryAssignReviewerBlockedReasonMatchesGuidance()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(
            scope.ServiceProvider,
            scenario);

        Assert.False(details.Actions.CanAssignReviewer);
        Assert.NotNull(details.Actions.AssignReviewerBlockedReason);

        string? expected = DiplomaWorkflowGuidance.BuildAssignReviewerBlockedReason(
            showSection: true,
            hasApprovedTopic: false,
            hasEmployees: true,
            topicVersions: [],
            reviewAssignmentStatus: ReviewAssignmentStatus.NotAssigned);

        Assert.Equal(expected, details.Actions.AssignReviewerBlockedReason);
    }

    [SkippableFact]
    public async Task AfterTopicApproval_SecretaryAdmitBlockedReasonMatchesGuidance()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunTopicApprovalAsync(services, scenario);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        Assert.False(details.Actions.CanAdmit);
        Assert.NotNull(details.Actions.AdmitBlockedReason);

        Diploma diploma = await IntegrationScenarioAssertions.GetWritableDiplomaAsync(
            services,
            scenario);
        string? expected = DiplomaWorkflowGuidance.BuildAdmitBlockedReason(
            showSection: true,
            sessionActive: true,
            diploma,
            diploma.TopicVersions,
            diploma.AdmissionStepAttempts);

        Assert.Equal(expected, details.Actions.AdmitBlockedReason);
    }

    [SkippableFact]
    public async Task BeforeAdmissionReview_SecretaryOverrideStepBlockedReasonMatchesGuidance()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;

        await WorkflowScenarioRunner.RunTopicApprovalAsync(services, scenario);

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        Assert.False(details.Actions.CanOverrideAdmissionStep);
        Assert.NotNull(details.Actions.OverrideAdmissionStepBlockedReason);

        Diploma diploma = await IntegrationScenarioAssertions.GetWritableDiplomaAsync(
            services,
            scenario);
        string? expected = DiplomaWorkflowGuidance.BuildOverrideAdmissionStepBlockedReason(
            showSection: true,
            sessionActive: true,
            admissionReviewStarted: false,
            diploma,
            diploma.AdmissionStepAttempts);

        Assert.Equal(expected, details.Actions.OverrideAdmissionStepBlockedReason);
    }

    [SkippableFact]
    public async Task AfterPartialCheckpoints_SecretaryAdmitBlockedReasonMatchesGuidance()
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

        DiplomaDetailsDto details = await IntegrationScenarioAssertions.GetDiplomaDetailsAsync(services, scenario);
        Assert.False(details.Actions.CanAdmit);
        Assert.NotNull(details.Actions.AdmitBlockedReason);

        Diploma diploma = await IntegrationScenarioAssertions.GetWritableDiplomaAsync(services, scenario);
        string? expected = DiplomaWorkflowGuidance.BuildAdmitBlockedReason(
            showSection: true,
            sessionActive: true,
            diploma,
            diploma.TopicVersions,
            diploma.AdmissionStepAttempts);

        Assert.Equal(expected, details.Actions.AdmitBlockedReason);
    }
}
