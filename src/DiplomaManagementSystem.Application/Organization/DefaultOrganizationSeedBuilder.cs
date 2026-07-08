using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Domain.Entities;

namespace DiplomaManagementSystem.Application.Organization;

public static class DefaultOrganizationSeedBuilder
{
    public static (Faculty Faculty, Department Department) FromOptions(OrganizationOptions options, DateTimeOffset createdAt)
    {
        string facultyName = NormalizeRequired(options.FacultyName, "Факультет");
        string departmentName = NormalizeRequired(options.DepartmentName, "Кафедра");

        Faculty faculty = new()
        {
            Id = Guid.NewGuid(),
            Name = facultyName,
            IsActive = true,
            CreatedAt = createdAt,
        };

        Department department = new()
        {
            Id = Guid.NewGuid(),
            FacultyId = faculty.Id,
            Faculty = faculty,
            Name = departmentName,
            SpecialtyCode = options.SpecialtyCode.Trim(),
            SpecialtyName = options.SpecialtyName.Trim(),
            StudyForm = string.IsNullOrWhiteSpace(options.StudyForm)
                ? "очної форми навчання"
                : options.StudyForm.Trim(),
            IsActive = true,
            CreatedAt = createdAt,
        };

        faculty.Departments.Add(department);

        return (faculty, department);
    }

    private static string NormalizeRequired(string value, string fallbackLabel)
    {
        string trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallbackLabel : trimmed;
    }
}
