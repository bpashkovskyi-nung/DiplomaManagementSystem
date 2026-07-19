using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Support;

internal static class IntegrationScenarioAssertions
{
    public static async Task AssertDiplomaCountInSessionAsync(
        IServiceProvider services,
        Guid sessionId,
        int expectedCount,
        CancellationToken cancellationToken = default)
    {
        IDiplomaQueries diplomaQueries = services.GetRequiredService<IDiplomaQueries>();
        int actualCount = (await diplomaQueries.ListForSessionReadAsync(sessionId, cancellationToken)).Count;
        Assert.Equal(expectedCount, actualCount);
    }

    public static async Task<DiplomaDetailsDto> GetDiplomaDetailsAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        ISecretaryDiplomaDetailsService detailsService = services.GetRequiredService<ISecretaryDiplomaDetailsService>();
        DiplomaDetailsDto? details = await detailsService.GetDetailsAsync(
            scenario.SessionId,
            scenario.DiplomaId,
            cancellationToken);

        Assert.NotNull(details);
        return details;
    }

    public static async Task<Domain.Entities.Diploma> GetWritableDiplomaAsync(
        IServiceProvider services,
        IntegrationScenario scenario,
        CancellationToken cancellationToken = default)
    {
        IDiplomaQueries diplomaQueries = services.GetRequiredService<IDiplomaQueries>();
        Domain.Entities.Diploma? diploma = await diplomaQueries.FindWritableAsync(
            new DiplomaWritableCriteria(
                scenario.DiplomaId,
                SessionId: scenario.SessionId,
                IncludeTopicVersions: true,
                IncludeAdmissionAttempts: true),
            cancellationToken);

        Assert.NotNull(diploma);
        return diploma;
    }

    public static async Task<MyDiplomaDto> GetStudentDiplomaAsync(
        IServiceProvider services,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        IStudentDiplomaService studentDiplomaService = services.GetRequiredService<IStudentDiplomaService>();
        return await studentDiplomaService.GetMyDiplomaAsync(studentId, cancellationToken);
    }

    public static void AssertTopicApproved(DiplomaDetailsDto details)
    {
        Assert.Equal(DiplomaLifecycleStatus.TopicApproved, details.State.LifecycleStatus);
        Assert.Contains(details.History.TopicVersions, version => version.Status == TopicVersionStatus.Approved);
        Assert.Empty(details.History.AttemptHistory);
        Assert.Equal(ReviewAssignmentStatus.NotAssigned, details.Assignments.ReviewAssignmentStatus);
    }

    public static void AssertTopicRejectedBySupervisor(DiplomaDetailsDto details)
    {
        Assert.Equal(DiplomaLifecycleStatus.TopicInReview, details.State.LifecycleStatus);
        Assert.Contains(
            details.History.TopicVersions,
            version => version.Status == TopicVersionStatus.Rejected);
        Assert.DoesNotContain(
            details.History.TopicVersions,
            version => version.Status == TopicVersionStatus.Approved);
    }

    public static void AssertMyDiplomaCompositeShape(MyDiplomaDto dto)
    {
        Assert.True(dto.Header.HasDiploma);
        Assert.NotNull(dto.Header.DiplomaId);
        Assert.False(string.IsNullOrWhiteSpace(dto.Header.SessionLabel));
        Assert.NotNull(dto.Assignments.SupervisorId);
        Assert.False(string.IsNullOrWhiteSpace(dto.Assignments.SupervisorName));
        Assert.NotNull(dto.State);
        Assert.NotEmpty(dto.History.TopicVersions);
        Assert.NotNull(dto.Actions);
        Assert.NotNull(dto.WorkflowProgress);
        Assert.NotNull(dto.Documents);
        Assert.NotEmpty(dto.SupervisorPool);
    }

    public static async Task AssertAuditLogExistsAsync(
        IServiceProvider services,
        Guid entityId,
        string action,
        Guid performedById,
        CancellationToken cancellationToken = default)
    {
        IApplicationDbContext dbContext = services.GetRequiredService<IApplicationDbContext>();
        bool exists = await dbContext.AuditLogs.AnyAsync(
            log => log.EntityId == entityId
                   && log.Action == action
                   && log.PerformedById == performedById,
            cancellationToken);

        Assert.True(exists, $"Expected audit log action '{action}' for entity {entityId}.");
    }

    public static async Task AssertAuditLogExistsByActionAsync(
        IServiceProvider services,
        string action,
        Guid performedById,
        CancellationToken cancellationToken = default)
    {
        IApplicationDbContext dbContext = services.GetRequiredService<IApplicationDbContext>();
        bool exists = await dbContext.AuditLogs.AnyAsync(
            log => log.Action == action && log.PerformedById == performedById,
            cancellationToken);

        Assert.True(exists, $"Expected audit log action '{action}' by user {performedById}.");
    }

    public static void AssertTopicRejected(DiplomaDetailsDto details)
    {
        Assert.Equal(DiplomaLifecycleStatus.TopicInReview, details.State.LifecycleStatus);
        Assert.Contains(
            details.History.TopicVersions,
            version => version.Status == TopicVersionStatus.Rejected);
        Assert.DoesNotContain(
            details.History.TopicVersions,
            version => version.Status == TopicVersionStatus.Approved);
    }

    public static void AssertAdmitted(DiplomaDetailsDto details)
    {
        Assert.Equal(DiplomaLifecycleStatus.Admitted, details.State.LifecycleStatus);
        Assert.Equal(DiplomaAdmissionStatus.Admitted, details.State.AdmissionStatus);
        Assert.NotNull(details.State.DefenceDate);
    }

    public static void AssertEmptyMyDiploma(MyDiplomaDto dto)
    {
        Assert.False(dto.Header.HasDiploma);
        Assert.Null(dto.Header.DiplomaId);
        Assert.False(string.IsNullOrWhiteSpace(dto.Header.SessionLabel));
        Assert.Null(dto.State);
        Assert.Empty(dto.History.TopicVersions);
        Assert.Empty(dto.History.Checkpoints);
        Assert.Empty(dto.History.Comments);
        Assert.Null(dto.Documents);
        Assert.Empty(dto.SupervisorPool);
    }
}
