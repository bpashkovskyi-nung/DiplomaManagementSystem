using System.Text.Json;
using System.Text.Json.Serialization;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport.Dtos;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport;

internal sealed class OrganizationStructureImportService(IApplicationDbContext dbContext)
    : IOrganizationStructureImportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<OrganizationStructureImportResultDto> ImportAsync(
        Stream jsonStream,
        OrganizationStructureImportMode mode,
        CancellationToken cancellationToken = default)
    {
        List<OrganizationStructureImportFacultyDto> faculties;
        try
        {
            faculties = await JsonSerializer.DeserializeAsync<List<OrganizationStructureImportFacultyDto>>(
                            jsonStream,
                            JsonOptions,
                            cancellationToken)
                        ?? [];
        }
        catch (JsonException exception)
        {
            throw new DomainException($"Некоректний JSON: {exception.Message}");
        }

        if (faculties.Count == 0)
        {
            throw new DomainException("JSON має містити принаймні один факультет.");
        }

        int facultiesCreated = 0;
        int facultiesUpdated = 0;
        int facultiesSkipped = 0;
        int departmentsCreated = 0;
        int departmentsUpdated = 0;
        int departmentsSkipped = 0;
        List<string> errors = [];

        foreach (OrganizationStructureImportFacultyDto facultyDto in faculties)
        {
            if (string.IsNullOrWhiteSpace(facultyDto.Name))
            {
                errors.Add("Пропущено факультет без назви.");
                continue;
            }

            if (facultyDto.Departments is null || facultyDto.Departments.Count == 0)
            {
                errors.Add($"Факультет «{facultyDto.Name.Trim()}» не містить кафедр.");
                continue;
            }

            string facultyName = facultyDto.Name.Trim();
            Faculty? faculty = await dbContext.Faculties
                .FirstOrDefaultAsync(item => item.Name == facultyName, cancellationToken);

            if (faculty is null)
            {
                faculty = new Faculty
                {
                    Id = Guid.NewGuid(),
                    Name = facultyName,
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                dbContext.Faculties.Add(faculty);
                facultiesCreated++;
            }
            else if (mode == OrganizationStructureImportMode.Upsert)
            {
                faculty.IsActive = true;
                facultiesUpdated++;
            }
            else
            {
                facultiesSkipped++;
            }

            foreach (OrganizationStructureImportDepartmentDto departmentDto in facultyDto.Departments)
            {
                if (string.IsNullOrWhiteSpace(departmentDto.Name))
                {
                    errors.Add($"Пропущено кафедру без назви у факультеті «{facultyName}».");
                    continue;
                }

                string departmentName = departmentDto.Name.Trim();
                Department? department = await dbContext.Departments
                    .FirstOrDefaultAsync(
                        item => item.FacultyId == faculty.Id && item.Name == departmentName,
                        cancellationToken);

                if (department is null)
                {
                    dbContext.Departments.Add(new Department
                    {
                        Id = Guid.NewGuid(),
                        FacultyId = faculty.Id,
                        Name = departmentName,
                        SpecialtyCode = departmentDto.SpecialtyCode?.Trim() ?? string.Empty,
                        SpecialtyName = departmentDto.SpecialtyName?.Trim() ?? string.Empty,
                        StudyForm = departmentDto.StudyForm?.Trim() ?? string.Empty,
                        IsActive = true,
                        CreatedAt = DateTimeOffset.UtcNow,
                    });
                    departmentsCreated++;
                }
                else if (mode == OrganizationStructureImportMode.Upsert)
                {
                    department.SpecialtyCode = departmentDto.SpecialtyCode?.Trim() ?? department.SpecialtyCode;
                    department.SpecialtyName = departmentDto.SpecialtyName?.Trim() ?? department.SpecialtyName;
                    department.StudyForm = departmentDto.StudyForm?.Trim() ?? department.StudyForm;
                    department.IsActive = true;
                    departmentsUpdated++;
                }
                else
                {
                    departmentsSkipped++;
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new OrganizationStructureImportResultDto(
            facultiesCreated,
            facultiesUpdated,
            facultiesSkipped,
            departmentsCreated,
            departmentsUpdated,
            departmentsSkipped,
            errors);
    }

    private sealed class OrganizationStructureImportFacultyDto
    {
        public string Name { get; set; } = string.Empty;

        public List<OrganizationStructureImportDepartmentDto>? Departments { get; set; }
    }

    private sealed class OrganizationStructureImportDepartmentDto
    {
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("specialtyCode")]
        public string? SpecialtyCode { get; set; }

        [JsonPropertyName("specialtyName")]
        public string? SpecialtyName { get; set; }

        [JsonPropertyName("studyForm")]
        public string? StudyForm { get; set; }
    }
}
