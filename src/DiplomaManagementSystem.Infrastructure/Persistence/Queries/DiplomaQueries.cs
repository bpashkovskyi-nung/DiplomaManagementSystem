using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Queries;

internal sealed class DiplomaQueries(ApplicationDbContext dbContext) : IDiplomaQueries
{
    public async Task<Diploma?> FindWritableAsync(
        DiplomaWritableCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Diploma> query = dbContext.Diplomas
            .Include(diploma => diploma.DefenceSession);

        if (criteria.IncludeAdmissionAttempts)
        {
            query = query.Include(diploma => diploma.AdmissionStepAttempts);
        }

        if (criteria.IncludeTopicVersions)
        {
            query = query.Include(diploma => diploma.TopicVersions);
        }

        query = query.Where(diploma => diploma.Id == criteria.DiplomaId);

        if (criteria.StudentId.HasValue)
        {
            query = query.Where(diploma => diploma.StudentId == criteria.StudentId.Value);
        }

        if (criteria.SupervisorId.HasValue)
        {
            query = query.Where(diploma => diploma.SupervisorId == criteria.SupervisorId.Value);
        }

        if (criteria.SessionId.HasValue)
        {
            query = query.Where(diploma => diploma.DefenceSessionId == criteria.SessionId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Diploma?> FindForAuthorizationAsync(Guid diplomaId, CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.DefenceSession)
            .FirstOrDefaultAsync(diploma => diploma.Id == diplomaId, cancellationToken);
    }

    public Task<Diploma?> FindDetailsReadAsync(
        Guid sessionId,
        Guid diplomaId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.DefenceSession)
            .Include(diploma => diploma.AdmissionStepAttempts)
            .Include(diploma => diploma.TopicVersions)
            .FirstOrDefaultAsync(
                diploma => diploma.Id == diplomaId && diploma.DefenceSessionId == sessionId,
                cancellationToken);
    }

    public Task<Diploma?> FindLatestForStudentReadAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.DefenceSession)
            .Include(diploma => diploma.TopicVersions)
            .Include(diploma => diploma.AdmissionStepAttempts)
            .Where(diploma => diploma.StudentId == studentId)
            .OrderByDescending(diploma => diploma.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<Diploma>> ListPendingCheckpointsByStepAsync(
        AdmissionStep step,
        Func<IQueryable<Diploma>, IQueryable<Diploma>> filter,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Diploma> query = dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.AdmissionStepAttempts)
            .Include(diploma => diploma.DefenceSession)
            .Where(diploma => diploma.DefenceSession.Status == DefenceSessionStatus.Active
                              && diploma.CurrentAdmissionStep == step);

        query = filter(query);

        return await query
            .OrderBy(diploma => diploma.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Diploma>> ListForSessionReadAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.AdmissionStepAttempts)
            .Include(diploma => diploma.TopicVersions)
            .Where(diploma => diploma.DefenceSessionId == sessionId)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Diploma>> ListAdmittedForSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Where(diploma => diploma.DefenceSessionId == sessionId
                              && diploma.AdmissionStatus == DiplomaAdmissionStatus.Admitted)
            .OrderBy(diploma => diploma.DefenceDate)
            .ToListAsync(cancellationToken);
    }

    public Task<List<DiplomaDashboardState>> ListDashboardStatesForSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Where(diploma => diploma.DefenceSessionId == sessionId)
            .Select(diploma => new DiplomaDashboardState(
                diploma.LifecycleStatus,
                diploma.CurrentAdmissionStep))
            .ToListAsync(cancellationToken);
    }

    public Task<List<Diploma>> ListReviewerQueueAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.AdmissionStepAttempts)
            .Include(diploma => diploma.DefenceSession)
            .Where(diploma => diploma.ReviewerId == reviewerId
                              && diploma.ReviewAssignmentStatus == ReviewAssignmentStatus.Assigned
                              && diploma.CurrentAdmissionStep == AdmissionStep.ExternalReview
                              && diploma.DefenceSession.Status == DefenceSessionStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> HasApprovedTopicAsync(Guid diplomaId, CancellationToken cancellationToken = default)
    {
        return dbContext.DiplomaTopicVersions.AnyAsync(
            version => version.DiplomaId == diplomaId && version.Status == TopicVersionStatus.Approved,
            cancellationToken);
    }

    public Task<List<Diploma>> ListPendingSupervisorStudentsAsync(
        Guid supervisorId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.DefenceSession)
            .Where(diploma => diploma.SupervisorId == supervisorId
                              && diploma.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Pending
                              && diploma.DefenceSession.Status == DefenceSessionStatus.Active)
            .OrderBy(diploma => diploma.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<List<Diploma>> ListForSupervisorReadAsync(
        Guid supervisorId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.AdmissionStepAttempts)
            .Include(diploma => diploma.TopicVersions)
            .Include(diploma => diploma.DefenceSession)
            .Where(diploma => diploma.SupervisorId == supervisorId
                              && diploma.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Confirmed
                              && diploma.DefenceSession.Status == DefenceSessionStatus.Active)
            .ToListAsync(cancellationToken);
    }

    public Task<Diploma?> FindForSupervisorReadAsync(
        Guid supervisorId,
        Guid diplomaId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.AdmissionStepAttempts)
            .Include(diploma => diploma.TopicVersions)
            .Include(diploma => diploma.DefenceSession)
            .FirstOrDefaultAsync(
                diploma => diploma.Id == diplomaId
                           && diploma.SupervisorId == supervisorId
                           && diploma.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Confirmed
                           && diploma.DefenceSession.Status == DefenceSessionStatus.Active,
                cancellationToken);
    }

    public Task<List<Diploma>> ListForReviewerReadAsync(
        Guid reviewerId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Diplomas
            .AsNoTracking()
            .Include(diploma => diploma.AdmissionStepAttempts)
            .Include(diploma => diploma.TopicVersions)
            .Include(diploma => diploma.DefenceSession)
            .Where(diploma => diploma.ReviewerId == reviewerId
                              && diploma.DefenceSession.Status == DefenceSessionStatus.Active)
            .ToListAsync(cancellationToken);
    }
}
