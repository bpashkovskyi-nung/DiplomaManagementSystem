namespace DiplomaManagementSystem.Application.Options;

public sealed class DepartmentOptions
{
    public const string SectionName = "Department";

    public string SelectedDepartmentCookieName { get; set; } = "dms.dept";

    public string ImpersonationCookieName { get; set; } = "dms.dept.sa";

    public int DepartmentCookieExpirationDays { get; set; } = 30;
}
