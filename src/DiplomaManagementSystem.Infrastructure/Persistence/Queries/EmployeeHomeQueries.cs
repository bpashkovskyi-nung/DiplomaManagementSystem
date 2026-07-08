using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Queries;

internal sealed class EmployeeHomeQueries(ApplicationDbContext dbContext) : IEmployeeHomeQueries
{
    public Task<int> CountPendingSupervisorStudentsAsync(Guid supervisorId, CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .CountAsync(
                diploma => diploma.SupervisorId == supervisorId
                           && diploma.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Pending,
                cancellationToken);
    }

    public Task<int> CountPendingSupervisorTopicsAsync(Guid supervisorId, CancellationToken cancellationToken = default)
    {
        return dbContext.DiplomaTopicVersions
            .AsNoTracking()
            .CountAsync(
                version => version.Status == TopicVersionStatus.PendingSupervisor
                           && version.Diploma.SupervisorId == supervisorId,
                cancellationToken);
    }

    public Task<bool> HasAnySupervisorDiplomasAsync(
        Guid supervisorId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .AnyAsync(
                diploma => diploma.SupervisorId == supervisorId
                           && (departmentId == null || diploma.DefenceSession.DepartmentId == departmentId),
                cancellationToken);
    }

    public Task<int> CountPendingHeadTopicsAsync(
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken = default)
    {
        if (sessionIds.Count == 0)
        {
            return Task.FromResult(0);
        }

        return dbContext.DiplomaTopicVersions
            .AsNoTracking()
            .CountAsync(
                version => version.Status == TopicVersionStatus.PendingHead
                           && sessionIds.Contains(version.Diploma.DefenceSessionId),
                cancellationToken);
    }

    public Task<int> CountPendingSupervisorFeedbackAsync(Guid supervisorId, CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .CountAsync(
                diploma => diploma.SupervisorId == supervisorId
                           && diploma.CurrentAdmissionStep == AdmissionStep.SupervisorFeedback,
                cancellationToken);
    }

    public Task<int> CountPendingReviewerAssignmentsAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .CountAsync(
                diploma => diploma.ReviewerId == reviewerId
                           && diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.Assigned
                           && diploma.CurrentAdmissionStep == AdmissionStep.ExternalReview,
                cancellationToken);
    }

    public Task<bool> HasAnyReviewerDiplomasAsync(
        Guid reviewerId,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .AnyAsync(
                diploma => diploma.ReviewerId == reviewerId
                           && (departmentId == null || diploma.DefenceSession.DepartmentId == departmentId),
                cancellationToken);
    }

    public Task<int> CountPendingAntiPlagiarismAsync(
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken = default)
    {
        if (sessionIds.Count == 0)
        {
            return Task.FromResult(0);
        }

        return dbContext.Diplomas
            .AsNoTracking()
            .CountAsync(
                diploma => sessionIds.Contains(diploma.DefenceSessionId)
                           && diploma.CurrentAdmissionStep == AdmissionStep.AntiPlagiarismClearance,
                cancellationToken);
    }

    public Task<int> CountPendingFormattingReviewAsync(
        IReadOnlyCollection<Guid> sessionIds,
        CancellationToken cancellationToken = default)
    {
        if (sessionIds.Count == 0)
        {
            return Task.FromResult(0);
        }

        return dbContext.Diplomas
            .AsNoTracking()
            .CountAsync(
                diploma => sessionIds.Contains(diploma.DefenceSessionId)
                           && diploma.CurrentAdmissionStep == AdmissionStep.FormattingReview,
                cancellationToken);
    }
}
