using DiplomaManagementSystem.Domain.Entities;

namespace DiplomaManagementSystem.Application.Tests.Departments;

internal static class OrganizationTestData
{
    public static async Task<Guid> SeedDepartmentAsync(
        DiplomaManagementSystem.Infrastructure.Persistence.ApplicationDbContext dbContext,
        string facultyName = "Факультет тестовий",
        string departmentName = "Кафедра тестова")
    {
        (Guid departmentId, _) = await SeedDepartmentWithSpecialtyAsync(
            dbContext,
            facultyName,
            departmentName);

        return departmentId;
    }

    public static async Task<(Guid DepartmentId, Guid SpecialtyId)> SeedDepartmentWithSpecialtyAsync(
        DiplomaManagementSystem.Infrastructure.Persistence.ApplicationDbContext dbContext,
        string facultyName = "Факультет тестовий",
        string departmentName = "Кафедра тестова",
        string specialtyCode = "123",
        string specialtyName = "Тестова спеціальність")
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
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        Specialty specialty = new()
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            Code = specialtyCode,
            Name = specialtyName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Faculties.Add(faculty);
        dbContext.Departments.Add(department);
        dbContext.Specialties.Add(specialty);
        await dbContext.SaveChangesAsync();

        return (department.Id, specialty.Id);
    }
}
