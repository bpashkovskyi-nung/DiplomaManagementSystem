using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Entities;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Employee;

internal sealed class DepartmentProgressReportService(
    IApplicationDbContext dbContext,
    CurrentDepartmentResolver currentDepartmentResolver,
    IUserDisplayQueries userDisplayQueries) : IDepartmentProgressReportService
{
    private const string MissingLabel = "—";
    private const string UnassignedSupervisor = "Без керівника";

    public async Task<IReadOnlyList<(Guid SessionId, string Label)>> ListDepartmentSessionsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        Guid departmentId = await currentDepartmentResolver.ResolveRequiredEmployeeDepartmentIdAsync(
            employeeId,
            cancellationToken);

        List<DefenceSession> sessions = await dbContext.DefenceSessions
            .AsNoTracking()
            .Where(session => session.DepartmentId == departmentId)
            .OrderByDescending(session => session.Year)
            .ThenByDescending(session => session.CreatedAt)
            .ToListAsync(cancellationToken);

        return sessions
            .Select(session => (
                session.Id,
                SecretarySessionLabel.Format(session.Year, session.Type, session.Semester)))
            .ToList();
    }

    public async Task<DepartmentProgressReportDto?> GetReportAsync(
        Guid employeeId,
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        Guid departmentId = await currentDepartmentResolver.ResolveRequiredEmployeeDepartmentIdAsync(
            employeeId,
            cancellationToken);

        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == sessionId && item.DepartmentId == departmentId,
                cancellationToken);

        if (session is null)
        {
            return null;
        }

        List<DefenceSessionMilestone> milestones = await dbContext.DefenceSessionMilestones
            .AsNoTracking()
            .Where(item => item.DefenceSessionId == sessionId)
            .OrderBy(item => item.Ordinal)
            .ToListAsync(cancellationToken);

        List<Diploma> diplomas = await dbContext.Diplomas
            .AsNoTracking()
            .Where(item => item.DefenceSessionId == sessionId)
            .ToListAsync(cancellationToken);

        HashSet<Guid> diplomaIds = diplomas.Select(item => item.Id).ToHashSet();
        List<DiplomaMilestoneProgress> progressRows = diplomaIds.Count == 0
            ? []
            : await dbContext.DiplomaMilestoneProgressEntries
                .AsNoTracking()
                .Where(item => diplomaIds.Contains(item.DiplomaId))
                .ToListAsync(cancellationToken);

        Dictionary<(Guid DiplomaId, Guid MilestoneId), int> progress = progressRows
            .ToDictionary(item => (item.DiplomaId, item.MilestoneId), item => item.ActualPercent);

        HashSet<Guid> userIds = diplomas
            .Select(item => item.StudentId)
            .Concat(diplomas.Where(item => item.SupervisorId.HasValue).Select(item => item.SupervisorId!.Value))
            .ToHashSet();

        Dictionary<Guid, ApplicationUser> users = await userDisplayQueries.LoadUsersAsync(userIds, cancellationToken);

        HashSet<Guid> groupIds = users.Values
            .Where(user => user.StudyGroupId.HasValue)
            .Select(user => user.StudyGroupId!.Value)
            .ToHashSet();

        Dictionary<Guid, string> groupNames = await userDisplayQueries.LoadStudyGroupNamesAsync(
            groupIds,
            cancellationToken);

        List<DepartmentProgressSupervisorGroupDto> groups = diplomas
            .GroupBy(diploma => diploma.SupervisorId)
            .Select(group =>
            {
                string supervisorName = UnassignedSupervisor;
                if (group.Key.HasValue && users.TryGetValue(group.Key.Value, out ApplicationUser? supervisor))
                {
                    supervisorName = supervisor.FullName;
                }

                List<DepartmentProgressStudentDto> students = group
                    .Select(diploma =>
                    {
                        users.TryGetValue(diploma.StudentId, out ApplicationUser? student);
                        string groupName = MissingLabel;
                        if (student?.StudyGroupId is { } studyGroupId
                            && groupNames.TryGetValue(studyGroupId, out string? name))
                        {
                            groupName = name;
                        }

                        return new DepartmentProgressStudentDto(
                            diploma.Id,
                            student?.FullName ?? MissingLabel,
                            groupName,
                            milestones.Select(milestone =>
                                progress.TryGetValue((diploma.Id, milestone.Id), out int percent)
                                    ? percent
                                    : (int?)null).ToList());
                    })
                    .OrderBy(item => PersonNameSort.SurnameKey(item.StudentFullName), StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(item => item.StudentFullName, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();

                return new DepartmentProgressSupervisorGroupDto(supervisorName, students);
            })
            .OrderBy(group => group.SupervisorName == UnassignedSupervisor)
            .ThenBy(group => PersonNameSort.SurnameKey(group.SupervisorName), StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(group => group.SupervisorName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new DepartmentProgressReportDto(
            session.Id,
            SecretarySessionLabel.Format(session.Year, session.Type, session.Semester),
            milestones.Select(item => new MilestoneColumnDto(item.Id, item.Ordinal, item.DueDate, item.ExpectedPercent)).ToList(),
            groups);
    }
}
