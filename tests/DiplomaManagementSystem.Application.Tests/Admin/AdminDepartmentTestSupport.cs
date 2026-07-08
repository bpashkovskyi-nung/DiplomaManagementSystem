using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Application.Tests.Admin;

internal static class AdminDepartmentTestSupport
{
    public static void RegisterDepartmentServices(IServiceCollection services)
    {
        services.AddSingleton<TestDepartmentContext>();
        services.AddSingleton<IDepartmentContext>(provider => provider.GetRequiredService<TestDepartmentContext>());
        services.AddScoped<IDepartmentAuthorizationService, DepartmentAuthorizationService>();
        services.AddScoped<CurrentDepartmentResolver>();
    }

    public static async Task<Guid> SeedDefaultDepartmentAsync(ApplicationDbContext dbContext) =>
        await OrganizationTestData.SeedDepartmentAsync(dbContext);
}
