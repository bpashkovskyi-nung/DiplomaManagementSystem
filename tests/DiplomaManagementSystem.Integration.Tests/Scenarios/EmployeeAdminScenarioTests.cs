using DiplomaManagementSystem.Application.Admin.Employees.Contracts;
using DiplomaManagementSystem.Application.Admin.Employees.Dtos;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Scenarios;

[Collection(nameof(IntegrationCollection))]
public sealed class EmployeeAdminScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task CreateAsync_GetDetailsAndUpdateAsync_PersistChanges()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        await new IntegrationScenarioBuilder(fixture.CreateProvider()).SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(scope.ServiceProvider);
        IEmployeeAdminService employeeAdminService = scope.ServiceProvider.GetRequiredService<IEmployeeAdminService>();

        string suffix = Guid.NewGuid().ToString("N")[..8];
        string email = $"employee.new.{suffix}@test.local";
        Guid employeeId = await employeeAdminService.CreateAsync(
            new EmployeeFormDto(null, "Employee New", email),
            CancellationToken.None);

        EmployeeDetailsDto? details = await employeeAdminService.GetDetailsAsync(employeeId, CancellationToken.None);
        Assert.NotNull(details);
        Assert.Equal("Employee New", details.FullName);
        Assert.Equal(email, details.Email);
        Assert.False(details.HasAssignments);

        string updatedEmail = $"employee.updated.{suffix}@test.local";
        await employeeAdminService.UpdateAsync(
            employeeId,
            new EmployeeFormDto(employeeId, "Employee Updated", updatedEmail),
            CancellationToken.None);

        details = await employeeAdminService.GetDetailsAsync(employeeId, CancellationToken.None);
        Assert.NotNull(details);
        Assert.Equal("Employee Updated", details.FullName);
        Assert.Equal(updatedEmail, details.Email);
    }

    [SkippableFact]
    public async Task GetAllAsync_IncludesSeededEmployees()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        IntegrationScenario scenario = await new IntegrationScenarioBuilder(fixture.CreateProvider())
            .SeedFullScenarioAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IEmployeeAdminService employeeAdminService = scope.ServiceProvider.GetRequiredService<IEmployeeAdminService>();

        IReadOnlyList<EmployeeListItemDto> employees = await employeeAdminService.GetAllAsync(CancellationToken.None);

        Assert.Contains(employees, employee => employee.Id == scenario.SupervisorId);
        Assert.Contains(employees, employee => employee.Id == scenario.SecretaryId);
    }
}
