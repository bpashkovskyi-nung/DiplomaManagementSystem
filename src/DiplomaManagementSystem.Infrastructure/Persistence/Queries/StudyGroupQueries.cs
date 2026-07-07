using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Queries;

internal sealed class StudyGroupQueries(ApplicationDbContext dbContext) : IStudyGroupQueries
{
    public Task<List<StudyGroupOption>> ListOptionsForSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.StudyGroups
            .AsNoTracking()
            .Where(group => group.DefenceSessionId == sessionId)
            .OrderBy(group => group.Name)
            .Select(group => new StudyGroupOption(group.Id, group.Name, group.Course))
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetNameAsync(Guid studyGroupId, CancellationToken cancellationToken = default)
    {
        return await dbContext.StudyGroups
            .AsNoTracking()
            .Where(group => group.Id == studyGroupId)
            .Select(group => group.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
