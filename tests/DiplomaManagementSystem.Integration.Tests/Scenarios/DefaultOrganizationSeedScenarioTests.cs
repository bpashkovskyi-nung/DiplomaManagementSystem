using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Infrastructure.Persistence;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class DefaultOrganizationSeedScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task AfterMigrate_DefaultFacultyAndDepartmentExist()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Faculty faculty = await dbContext.Faculties.SingleAsync();
        Department department = await dbContext.Departments.SingleAsync();
        Specialty specialty = await dbContext.Specialties.SingleAsync();

        Assert.Equal("факультет інформаційних технологій", faculty.Name);
        Assert.Equal("кафедра комп'ютерних систем і мереж", department.Name);
        Assert.Equal(faculty.Id, department.FacultyId);
        Assert.Equal("123", specialty.Code);
        Assert.Equal(department.Id, specialty.DepartmentId);

        bool allSessionsAssigned = await dbContext.DefenceSessions
            .AllAsync(session => session.DepartmentId == department.Id);
        Assert.True(allSessionsAssigned);
    }
}
