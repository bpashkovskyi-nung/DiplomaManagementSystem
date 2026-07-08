using DiplomaManagementSystem.Application.AdminPreview.Contracts;
using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.AdminPreview;

internal sealed class AdminPreviewUserPickerService(
    IApplicationDbContext dbContext,
    CurrentDepartmentResolver currentDepartmentResolver) : IAdminPreviewUserPickerService
{
    public async Task<IReadOnlyList<AdminPreviewUserOption>> GetUsersAsync(
        UserKind userKind,
        CancellationToken cancellationToken = default)
    {
        Guid? scopedDepartmentId = await currentDepartmentResolver.TryResolveScopedDepartmentIdAsync(cancellationToken);

        if (userKind == UserKind.Student)
        {
            List<ApplicationUser> students = await dbContext.Users
                .AsNoTracking()
                .Include(user => user.StudyGroup)
                .Include(user => user.DefenceSession)
                .Where(user => user.UserKind == UserKind.Student)
                .ToListAsync(cancellationToken);

            return students
                .OrderBy(student => PersonNameSort.SurnameKey(student.FullName), StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(student => student.FullName, StringComparer.CurrentCultureIgnoreCase)
                .Select(student => new AdminPreviewUserOption(
                    student.Id,
                    student.FullName,
                    student.Email ?? string.Empty,
                    BuildStudentSubtitle(student)))
                .ToList();
        }

        IQueryable<ApplicationUser> employeeQuery = dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserKind == UserKind.Employee);

        if (scopedDepartmentId is Guid departmentId)
        {
            IQueryable<Guid> scopedEmployeeIds = dbContext.DepartmentEmployees
                .AsNoTracking()
                .Where(employee => employee.DepartmentId == departmentId && employee.IsActive)
                .Select(employee => employee.UserId);

            employeeQuery = employeeQuery.Where(user => scopedEmployeeIds.Contains(user.Id));
        }

        List<EmployeeRow> employees = await employeeQuery
            .OrderBy(user => user.FullName)
            .Select(user => new EmployeeRow(user.Id, user.FullName, user.Email ?? string.Empty))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, List<string>> roleLabelsByEmployee =
            await BuildEmployeeRoleLabelsAsync(scopedDepartmentId, cancellationToken);

        return employees
            .Select(employee => new AdminPreviewUserOption(
                employee.Id,
                employee.FullName,
                employee.Email,
                roleLabelsByEmployee.TryGetValue(employee.Id, out List<string>? labels)
                    ? string.Join(" · ", labels)
                    : null))
            .ToList();
    }

    private async Task<Dictionary<Guid, List<string>>> BuildEmployeeRoleLabelsAsync(
        Guid? scopedDepartmentId,
        CancellationToken cancellationToken)
    {
        IQueryable<Domain.Entities.DefenceSession> sessionsQuery = dbContext.DefenceSessions.AsNoTracking();
        if (scopedDepartmentId is Guid departmentId)
        {
            sessionsQuery = sessionsQuery.Where(session => session.DepartmentId == departmentId);
        }

        List<RoleAssignmentRow> rows = await dbContext.AnnualRoleAssignments
            .AsNoTracking()
            .Join(
                sessionsQuery,
                assignment => assignment.DefenceSessionId,
                session => session.Id,
                (assignment, session) => new RoleAssignmentRow(
                    assignment.EmployeeId,
                    assignment.RoleType,
                    session.Year,
                    session.Type,
                    session.Semester))
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(row => row.EmployeeId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(row => $"{WorkflowUkrainianLabels.FormatAnnualRoleType(row.RoleType)} · {SecretarySessionLabel.Format(row.Year, row.Type, row.Semester)}")
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(label => label, StringComparer.CurrentCulture)
                    .ToList());
    }

    private static string? BuildStudentSubtitle(ApplicationUser student)
    {
        if (student.DefenceSession is null)
        {
            return student.StudyGroup?.Name;
        }

        string sessionLabel = SecretarySessionLabel.Format(
            student.DefenceSession.Year,
            student.DefenceSession.Type,
            student.DefenceSession.Semester);

        return student.StudyGroup is null
            ? sessionLabel
            : $"{sessionLabel} · {student.StudyGroup.Name}";
    }

    private sealed record EmployeeRow(Guid Id, string FullName, string Email);

    private sealed record RoleAssignmentRow(
        Guid EmployeeId,
        AnnualRoleType RoleType,
        int Year,
        DefenceSessionType Type,
        int? Semester);
}
