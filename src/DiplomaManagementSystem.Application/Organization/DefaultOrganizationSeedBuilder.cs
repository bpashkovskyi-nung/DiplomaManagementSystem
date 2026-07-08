using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Domain.Entities;

namespace DiplomaManagementSystem.Application.Organization;

public static class DefaultOrganizationSeedBuilder
{
    public static (Faculty Faculty, Department Department, Specialty Specialty) FromOptions(
        OrganizationOptions options,
        DateTimeOffset createdAt)
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
            IsActive = true,
            CreatedAt = createdAt,
        };

        Specialty specialty = new()
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            Department = department,
            Code = options.SpecialtyCode.Trim(),
            Name = string.IsNullOrWhiteSpace(options.SpecialtyName.Trim())
                ? departmentName
                : options.SpecialtyName.Trim(),
            IsActive = true,
            CreatedAt = createdAt,
        };

        faculty.Departments.Add(department);
        department.Specialties.Add(specialty);

        return (faculty, department, specialty);
    }

    private static string NormalizeRequired(string value, string fallbackLabel)
    {
        string trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallbackLabel : trimmed;
    }
}
