using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Web.Areas.Admin.Models;

namespace DiplomaManagementSystem.Web.Areas.Admin;

internal static class AdminNavigation
{
    public static IReadOnlyList<AdminNavLink> DefenceSessionsBack() =>
    [
        new(AdminPageTitles.DefenceSessions, "DefenceSessions", "Index"),
    ];

    public static IReadOnlyList<AdminNavLink> EmployeesBack() =>
    [
        new(AdminPageTitles.Employees, "Employees", "Index"),
    ];

    public static IReadOnlyList<AdminNavLink> SessionBack(Guid defenceSessionId) =>
    [
        new(AdminPageTitles.DefenceSession, "DefenceSessions", "Details", SessionRoute(defenceSessionId)),
    ];

    public static IReadOnlyList<AdminNavLink> StudentsBack(Guid defenceSessionId) =>
    [
        new(AdminPageTitles.Students, "Students", "Index", StudentsRoute(defenceSessionId)),
    ];

    private static Dictionary<string, string> SessionRoute(Guid defenceSessionId) =>
        new() { ["id"] = defenceSessionId.ToString() };

    private static Dictionary<string, string> StudentsRoute(Guid defenceSessionId) =>
        new() { ["defenceSessionId"] = defenceSessionId.ToString() };
}
