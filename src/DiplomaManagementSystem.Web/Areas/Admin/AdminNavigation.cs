using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Web.Areas.Admin.Models;

namespace DiplomaManagementSystem.Web.Areas.Admin;

internal static class AdminNavigation
{
    public static IReadOnlyList<AdminNavLink> Global() =>
    [
        new(AdminPageTitles.Home, "Home", "Index"),
        new(AdminPageTitles.DefenceSessions, "DefenceSessions", "Index"),
        new(AdminPageTitles.Employees, "Employees", "Index"),
    ];

    public static IReadOnlyList<AdminNavLink> EmployeesBack() =>
    [
        new(AdminPageTitles.Employees, "Employees", "Index"),
        new(AdminPageTitles.Home, "Home", "Index"),
    ];

    public static IReadOnlyList<AdminNavLink> DefenceSessionsBack() =>
    [
        new(AdminPageTitles.DefenceSessions, "DefenceSessions", "Index"),
        new(AdminPageTitles.Home, "Home", "Index"),
    ];

    public static IReadOnlyList<AdminNavLink> SessionContext(Guid defenceSessionId) =>
    [
        new(AdminPageTitles.DefenceSessions, "DefenceSessions", "Index"),
        new(AdminPageTitles.DefenceSession, "DefenceSessions", "Details", SessionRoute(defenceSessionId)),
        new(AdminPageTitles.Students, "Students", "Index", StudentsRoute(defenceSessionId)),
        new(AdminPageTitles.Home, "Home", "Index"),
    ];

    public static IReadOnlyList<AdminNavLink> StudentsBack(Guid defenceSessionId) =>
    [
        new(AdminPageTitles.Students, "Students", "Index", StudentsRoute(defenceSessionId)),
        new(AdminPageTitles.DefenceSession, "DefenceSessions", "Details", SessionRoute(defenceSessionId)),
        new(AdminPageTitles.Home, "Home", "Index"),
    ];

    private static Dictionary<string, string> SessionRoute(Guid defenceSessionId) =>
        new() { ["id"] = defenceSessionId.ToString() };

    private static Dictionary<string, string> StudentsRoute(Guid defenceSessionId) =>
        new() { ["defenceSessionId"] = defenceSessionId.ToString() };
}
