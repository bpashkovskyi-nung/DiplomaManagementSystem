using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin;

internal static class SuperAdminNavigation
{
    public static IReadOnlyList<SuperAdminNavLink> Global() =>
    [
        new(SuperAdminPageTitles.Faculties, "Faculties", "Index"),
        new(SuperAdminPageTitles.OrganizationImport, "OrganizationImport", "Index"),
    ];

    public static IReadOnlyList<SuperAdminNavLink> FacultiesFormBack() =>
    [
        new(SuperAdminPageTitles.Faculties, "Faculties", "Index"),
    ];

    public static IReadOnlyList<SuperAdminNavLink> FacultyDepartmentsBack() =>
        FacultiesFormBack();

    public static IReadOnlyList<SuperAdminNavLink> DepartmentFormBack(Guid facultyId, string facultyName) =>
    [
        new(SuperAdminPageTitles.Faculties, "Faculties", "Index"),
        new(
            facultyName,
            "Departments",
            "Index",
            new Dictionary<string, string> { ["facultyId"] = facultyId.ToString() }),
    ];
}
