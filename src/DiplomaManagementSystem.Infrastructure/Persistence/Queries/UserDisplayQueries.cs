using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Queries;

internal sealed class UserDisplayQueries(ApplicationDbContext dbContext) : IUserDisplayQueries
{
    private const string MissingLabel = "—";

    public async Task<Dictionary<Guid, ApplicationUser>> LoadUsersAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, cancellationToken);
    }

    public async Task<Dictionary<Guid, string>> LoadFullNamesAsync(
        IReadOnlyCollection<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Users
            .AsNoTracking()
            .Where(user => userIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.FullName, cancellationToken);
    }

    public async Task<Dictionary<Guid, string>> LoadStudyGroupNamesAsync(
        IReadOnlyCollection<Guid> studyGroupIds,
        CancellationToken cancellationToken = default)
    {
        if (studyGroupIds.Count == 0)
        {
            return [];
        }

        return await dbContext.StudyGroups
            .AsNoTracking()
            .Where(group => studyGroupIds.Contains(group.Id))
            .ToDictionaryAsync(group => group.Id, group => group.Name, cancellationToken);
    }

    public async Task<Dictionary<Guid, StudentDisplayInfo>> LoadStudentDisplaysAsync(
        IReadOnlyCollection<Guid> studentIds,
        CancellationToken cancellationToken = default)
    {
        if (studentIds.Count == 0)
        {
            return [];
        }

        Dictionary<Guid, ApplicationUser> students = await LoadUsersAsync(studentIds, cancellationToken);

        HashSet<Guid> groupIds = students.Values
            .Where(student => student.StudyGroupId.HasValue)
            .Select(student => student.StudyGroupId!.Value)
            .ToHashSet();

        Dictionary<Guid, string> groupNames = await LoadStudyGroupNamesAsync(groupIds, cancellationToken);

        return students.ToDictionary(
            pair => pair.Key,
            pair =>
            {
                string groupName = MissingLabel;
                if (pair.Value.StudyGroupId is Guid groupId
                    && groupNames.TryGetValue(groupId, out string? name))
                {
                    groupName = name;
                }

                return new StudentDisplayInfo(pair.Value.FullName, groupName);
            });
    }

    public Task<List<UserOption>> LoadEmployeeOptionsForDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.DepartmentEmployees
            .AsNoTracking()
            .Where(employee => employee.DepartmentId == departmentId && employee.IsActive)
            .Join(
                dbContext.Users.AsNoTracking().Where(user => user.UserKind == UserKind.Employee),
                employee => employee.UserId,
                user => user.Id,
                (employee, user) => new { employee, user })
            .OrderBy(pair => pair.employee.FullName)
            .Select(pair => new UserOption(
                pair.user.Id,
                pair.employee.FullName,
                pair.user.Email ?? string.Empty))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsActiveDepartmentEmployeeAsync(
        Guid userId,
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.DepartmentEmployees
            .AsNoTracking()
            .AnyAsync(
                employee => employee.UserId == userId
                            && employee.DepartmentId == departmentId
                            && employee.IsActive,
                cancellationToken);
    }

    public async Task<StudentStorageContext?> GetStudentStorageContextAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == studentId && user.StudyGroupId.HasValue)
            .Join(
                dbContext.StudyGroups.AsNoTracking(),
                user => user.StudyGroupId!.Value,
                group => group.Id,
                (user, group) => new StudentStorageContext(
                    user.Id,
                    group.Id,
                    group.Name,
                    user.FullName))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
