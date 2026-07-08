using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin;

internal static class SuperAdminNavigation
{
    public static IReadOnlyList<SuperAdminNavLink> Global() =>
    [
        new(SuperAdminPageTitles.Home, "Home", "Index"),
        new(SuperAdminPageTitles.Faculties, "Faculties", "Index"),
        new(SuperAdminPageTitles.Departments, "Departments", "Index"),
        new(SuperAdminPageTitles.DepartmentAdmins, "DepartmentAdmins", "Index"),
        new(SuperAdminPageTitles.OrganizationImport, "OrganizationImport", "Index"),
    ];

    public static IReadOnlyList<SuperAdminNavLink> FacultiesBack() =>
    [
        new(SuperAdminPageTitles.Faculties, "Faculties", "Index"),
        new(SuperAdminPageTitles.Home, "Home", "Index"),
    ];

    public static IReadOnlyList<SuperAdminNavLink> DepartmentsBack() =>
    [
        new(SuperAdminPageTitles.Departments, "Departments", "Index"),
        new(SuperAdminPageTitles.Home, "Home", "Index"),
    ];

    public static IReadOnlyList<SuperAdminNavLink> DepartmentAdminsBack(Guid? departmentId = null) =>
    [
        new(
            SuperAdminPageTitles.DepartmentAdmins,
            "DepartmentAdmins",
            "Index",
            departmentId is Guid id ? new Dictionary<string, string> { ["departmentId"] = id.ToString() } : null),
        new(SuperAdminPageTitles.Home, "Home", "Index"),
    ];

    public static IReadOnlyList<SuperAdminNavLink> OrganizationImportBack() =>
    [
        new(SuperAdminPageTitles.OrganizationImport, "OrganizationImport", "Index"),
        new(SuperAdminPageTitles.Home, "Home", "Index"),
    ];
}
