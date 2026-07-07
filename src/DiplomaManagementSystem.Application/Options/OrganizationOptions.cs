namespace DiplomaManagementSystem.Application.Options;

public sealed class OrganizationOptions
{
    public const string SectionName = "Organization";

    public string MinistryName { get; set; } = "МІНІСТЕРСТВО ОСВІТИ І НАУКИ УКРАЇНИ";

    public string UniversityName { get; set; } =
        "ІВАНО-ФРАНКІВСЬКИЙ НАЦІОНАЛЬНИЙ ТЕХНІЧНИЙ УНІВЕРСИТЕТ НАФТИ І ГАЗУ";

    public string City { get; set; } = "м. Івано-Франківськ";

    public string RectorName { get; set; } = string.Empty;

    public string SpecialtyCode { get; set; } = string.Empty;

    public string SpecialtyName { get; set; } = string.Empty;

    public string FacultyName { get; set; } = string.Empty;

    public string StudyForm { get; set; } = "очної форми навчання";

    public string DepartmentName { get; set; } = string.Empty;
}
