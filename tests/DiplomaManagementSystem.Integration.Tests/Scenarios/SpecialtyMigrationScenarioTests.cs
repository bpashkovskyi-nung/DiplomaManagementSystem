using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Infrastructure.Persistence;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class SpecialtyMigrationScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task AfterMigrate_EachDepartmentHasSpecialtyAndEachGroupHasAcademicFields()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        List<Department> departments = await dbContext.Departments.AsNoTracking().ToListAsync();
        Assert.NotEmpty(departments);

        foreach (Department department in departments)
        {
            int specialtyCount = await dbContext.Specialties
                .CountAsync(specialty => specialty.DepartmentId == department.Id);
            Assert.True(specialtyCount >= 1, $"Department {department.Name} has no specialties.");
        }

        List<StudyGroup> groups = await dbContext.StudyGroups.AsNoTracking().ToListAsync();
        foreach (StudyGroup group in groups)
        {
            Assert.NotEqual(Guid.Empty, group.SpecialtyId);
            Assert.False(string.IsNullOrWhiteSpace(group.StudyForm));

            bool specialtyExists = await dbContext.Specialties
                .AnyAsync(specialty => specialty.Id == group.SpecialtyId);
            Assert.True(specialtyExists, $"Study group {group.Name} references missing specialty.");
        }
    }
}
