using DiplomaManagementSystem.Domain.Entities;

namespace DiplomaManagementSystem.Application.Tests.Departments;

internal static class OrganizationTestData
{
    public static async Task<Guid> SeedDepartmentAsync(
        DiplomaManagementSystem.Infrastructure.Persistence.ApplicationDbContext dbContext,
        string facultyName = "Факультет тестовий",
        string departmentName = "Кафедра тестова")
    {
        Faculty faculty = new()
        {
            Id = Guid.NewGuid(),
            Name = facultyName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        Department department = new()
        {
            Id = Guid.NewGuid(),
            FacultyId = faculty.Id,
            Name = departmentName,
            SpecialtyCode = "123",
            SpecialtyName = "Тестова спеціальність",
            StudyForm = "очної форми навчання",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Faculties.Add(faculty);
        dbContext.Departments.Add(department);
        await dbContext.SaveChangesAsync();

        return department.Id;
    }
}
