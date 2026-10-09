using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Queries;

internal sealed class DefenceSessionQueries(ApplicationDbContext dbContext) : IDefenceSessionQueries
{
    public Task<bool> ExistsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.DefenceSessions.AnyAsync(session => session.Id == sessionId, cancellationToken);
    }

    public async Task<DefenceSessionType?> GetTypeAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await dbContext.DefenceSessions
            .AsNoTracking()
            .Where(session => session.Id == sessionId)
            .Select(session => (DefenceSessionType?)session.Type)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<DefenceSession?> FindReadAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);
    }

    public Task<DefenceSessionSummary?> FindSummaryAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return dbContext.DefenceSessions
            .AsNoTracking()
            .Where(session => session.Id == sessionId)
            .Select(session => new DefenceSessionSummary(
                session.Id,
                session.Year,
                session.Type,
                session.Semester))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<DefenceSessionSummary?> FindForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == studentId && user.DefenceSessionId != null)
            .Join(
                dbContext.DefenceSessions.AsNoTracking(),
                user => user.DefenceSessionId!.Value,
                session => session.Id,
                (_, session) => new DefenceSessionSummary(
                    session.Id,
                    session.Year,
                    session.Type,
                    session.Semester))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
