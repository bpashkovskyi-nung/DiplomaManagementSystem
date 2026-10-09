using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Infrastructure.Persistence;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class DefaultOrganizationSeedScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task AfterMigrate_DefaultFacultyAndDepartmentExist()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        await OrganizationSeedHelper.EnsureDefaultOrganizationAsync(services);

        ApplicationDbContext dbContext = services.GetRequiredService<ApplicationDbContext>();
        OrganizationOptions options = services.GetRequiredService<IOptions<OrganizationOptions>>().Value;

        Guid defaultDepartmentId = await IntegrationDepartmentHelper.GetDefaultDepartmentIdAsync(services);

        Department department = await dbContext.Departments
            .AsNoTracking()
            .SingleAsync(item => item.Id == defaultDepartmentId);

        Faculty faculty = await dbContext.Faculties
            .AsNoTracking()
            .SingleAsync(item => item.Id == department.FacultyId);

        Specialty specialty = await dbContext.Specialties
            .AsNoTracking()
            .Where(item => item.DepartmentId == department.Id && item.Code == options.SpecialtyCode)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .FirstAsync();

        Assert.Equal(options.FacultyName, faculty.Name);
        Assert.Equal(options.DepartmentName, department.Name);
        Assert.Equal(department.Id, specialty.DepartmentId);
        Assert.Equal(options.SpecialtyName, specialty.Name);
    }
}
