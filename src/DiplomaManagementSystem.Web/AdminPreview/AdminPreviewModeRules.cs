namespace DiplomaManagementSystem.Web.AdminPreview;

internal static class AdminPreviewModeRules
{
    public static AdminPreviewMode FromStoredValue(int stored) => stored switch
    {
        int value when Enum.IsDefined(typeof(AdminPreviewMode), value) => (AdminPreviewMode)value,
        _ => AdminPreviewMode.Admin,
    };

    public static AdminPreviewMode Normalize(AdminPreviewMode mode) => mode;

    public static bool IsHomeMode(AdminPreviewMode mode) =>
        mode is AdminPreviewMode.SuperAdmin or AdminPreviewMode.Admin;

    public static bool IsEmployeePreviewMode(AdminPreviewMode mode) =>
        mode == AdminPreviewMode.Employee;

    public static bool IsSecretaryPreviewMode(AdminPreviewMode mode) =>
        mode == AdminPreviewMode.Secretary;

    public static bool IsEmployeeArea(string? area) =>
        area is "Employee" or "Secretary";

    public static bool AreaMatchesMode(string area, AdminPreviewMode mode) => mode switch
    {
        AdminPreviewMode.SuperAdmin => area == "SuperAdmin",
        AdminPreviewMode.Admin => area == "Admin",
        AdminPreviewMode.Student => area == "Student",
        AdminPreviewMode.Secretary => area == "Secretary",
        AdminPreviewMode.Employee => area == "Employee",
        _ => false,
    };

    public static bool IsValidReturnUrlArea(string? area, AdminPreviewMode mode) => mode switch
    {
        AdminPreviewMode.SuperAdmin => area == "SuperAdmin",
        AdminPreviewMode.Admin => area == "Admin",
        AdminPreviewMode.Student => area == "Student",
        AdminPreviewMode.Secretary => area == "Secretary",
        AdminPreviewMode.Employee => area == "Employee",
        _ => false,
    };

    public static bool IsEmployeeSurface(AdminPreviewMode mode) =>
        mode is AdminPreviewMode.Secretary or AdminPreviewMode.Employee;
}
