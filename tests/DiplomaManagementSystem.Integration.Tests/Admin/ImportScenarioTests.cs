using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.Employees.Contracts;
using DiplomaManagementSystem.Application.Admin.Students.Contracts;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Import.Contracts;
using DiplomaManagementSystem.Application.Import.Models;
using DiplomaManagementSystem.Integration.Tests.Support;

using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Admin;

[Collection(nameof(IntegrationCollection))]
public sealed class ImportScenarioTests(PostgreSqlFixture fixture)
{
    [SkippableFact]
    public async Task StudentImport_CreatesStudentsAndDiplomas()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IntegrationScenarioBuilder builder = new(scope.ServiceProvider);
        Guid sessionId = await builder.SeedSessionOnlyAsync();

        IStudentImportService studentImportService = scope.ServiceProvider.GetRequiredService<IStudentImportService>();
        IStudentAdminService studentAdminService = scope.ServiceProvider.GetRequiredService<IStudentAdminService>();

        string suffix = Guid.NewGuid().ToString("N")[..8];

        await using MemoryStream csv = IntegrationTestCsv.Students(
            ("Іван Іваненко", $"ivan.import.{suffix}@example.com", "КН-41"),
            ("Марія Коваленко", $"maria.import.{suffix}@example.com", "КН-41"));

        ImportResult result = await studentImportService.ImportAsync(sessionId, csv, "students.csv");

        Assert.True(
            result.Errors.Count == 0,
            $"Import errors: {string.Join("; ", result.Errors)}");
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(2, (await studentAdminService.GetAllAsync(sessionId)).Count);
    }

    [SkippableFact]
    public async Task EmployeeImport_CreatesEmployees()
    {
        IntegrationTestGuards.RequireDatabase(fixture);
        await new IntegrationScenarioBuilder(fixture.CreateProvider()).SeedSessionOnlyAsync();

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IEmployeeImportService employeeImportService = scope.ServiceProvider.GetRequiredService<IEmployeeImportService>();
        IEmployeeAdminService employeeAdminService = scope.ServiceProvider.GetRequiredService<IEmployeeAdminService>();

        int initialCount = (await employeeAdminService.GetAllAsync()).Count;
        string suffix = Guid.NewGuid().ToString("N")[..8];

        await using MemoryStream csv = IntegrationTestCsv.Employees(
            ("Петро Петренко", $"petro.import.{suffix}@example.com"),
            ("Олена Шевченко", $"olena.import.{suffix}@example.com"));

        ImportResult result = await employeeImportService.ImportAsync(csv, "employees.csv");

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(2, result.ImportedCount);
        Assert.Empty(result.Errors);
        Assert.Equal(initialCount + 2, (await employeeAdminService.GetAllAsync()).Count);
    }

    [SkippableFact]
    public async Task StudentImport_PartialFailure_ImportsValidRowsOnly()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IntegrationScenarioBuilder builder = new(scope.ServiceProvider);
        Guid sessionId = await builder.SeedSessionOnlyAsync();

        IStudentImportService studentImportService = scope.ServiceProvider.GetRequiredService<IStudentImportService>();
        IStudentAdminService studentAdminService = scope.ServiceProvider.GetRequiredService<IStudentAdminService>();

        string suffix = Guid.NewGuid().ToString("N")[..8];

        await using MemoryStream csv = IntegrationTestCsv.Students(
            ("Іван Іваненко", $"ivan.partial.{suffix}@example.com", "КН-41"),
            ("Невалідний Рядок", "not-an-email", "КН-41"));

        ImportResult result = await studentImportService.ImportAsync(sessionId, csv, "students.csv");

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Single(result.Errors);
        Assert.Single(await studentAdminService.GetAllAsync(sessionId));
    }

    [SkippableFact]
    public async Task StudentImport_DuplicateEmail_SkipsSecondRow()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IntegrationScenarioBuilder builder = new(scope.ServiceProvider);
        Guid sessionId = await builder.SeedSessionOnlyAsync();

        IStudentImportService studentImportService = scope.ServiceProvider.GetRequiredService<IStudentImportService>();
        IStudentAdminService studentAdminService = scope.ServiceProvider.GetRequiredService<IStudentAdminService>();

        string suffix = Guid.NewGuid().ToString("N")[..8];
        string duplicateEmail = $"dup.{suffix}@example.com";

        await using MemoryStream csv = IntegrationTestCsv.Students(
            ("Перший Студент", duplicateEmail, "КН-41"),
            ("Другий Студент", duplicateEmail, "КН-42"));

        ImportResult result = await studentImportService.ImportAsync(sessionId, csv, "students.csv");

        Assert.Equal(2, result.TotalRows);
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Single(result.Errors);
        Assert.Contains("дубльована", result.Errors[0], StringComparison.OrdinalIgnoreCase);
        Assert.Single(await studentAdminService.GetAllAsync(sessionId));
    }

    [SkippableFact]
    public async Task StudentImport_WhenSessionNotFound_ReturnsError()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IStudentImportService studentImportService = scope.ServiceProvider.GetRequiredService<IStudentImportService>();

        await using MemoryStream csv = IntegrationTestCsv.Students(
            ("Іван Іваненко", "ivan@example.com", "КН-41"));

        ImportResult result = await studentImportService.ImportAsync(Guid.NewGuid(), csv, "students.csv");

        Assert.Equal(0, result.TotalRows);
        Assert.Single(result.Errors);
        Assert.Contains("не знайдено", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task StudentImport_WhenSessionArchived_ReturnsError()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IntegrationScenarioBuilder builder = new(scope.ServiceProvider);
        Guid sessionId = await builder.SeedSessionOnlyAsync();
        IDefenceSessionService defenceSessionService =
            scope.ServiceProvider.GetRequiredService<IDefenceSessionService>();
        IUserProvisioningService userProvisioningService =
            scope.ServiceProvider.GetRequiredService<IUserProvisioningService>();
        ApplicationUser secretary = await userProvisioningService.CreateEmployeeAsync(
            "Secretary",
            $"secretary.archive.{Guid.NewGuid():N}@test.local");
        await defenceSessionService.ArchiveAsync(sessionId, secretary.Id);

        IStudentImportService studentImportService = scope.ServiceProvider.GetRequiredService<IStudentImportService>();
        await using MemoryStream csv = IntegrationTestCsv.Students(
            ("Іван Іваненко", $"ivan.archive.{Guid.NewGuid():N}@example.com", "КН-41"));

        ImportResult result = await studentImportService.ImportAsync(sessionId, csv, "students.csv");

        Assert.Equal(0, result.TotalRows);
        Assert.Single(result.Errors);
        Assert.Contains("архів", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task StudentImport_UnsupportedFileFormat_ReturnsError()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IntegrationScenarioBuilder builder = new(scope.ServiceProvider);
        Guid sessionId = await builder.SeedSessionOnlyAsync();
        IStudentImportService studentImportService = scope.ServiceProvider.GetRequiredService<IStudentImportService>();

        await using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes("plain text"));
        ImportResult result = await studentImportService.ImportAsync(sessionId, stream, "students.txt");

        Assert.Equal(0, result.TotalRows);
        Assert.Single(result.Errors);
        Assert.Contains("формат", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public async Task EmployeeImport_UnsupportedFileFormat_ReturnsError()
    {
        IntegrationTestGuards.RequireDatabase(fixture);

        await using AsyncServiceScope scope = fixture.CreateProvider().CreateAsyncScope();
        IEmployeeImportService employeeImportService =
            scope.ServiceProvider.GetRequiredService<IEmployeeImportService>();

        await using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes("plain text"));
        ImportResult result = await employeeImportService.ImportAsync(stream, "employees.txt");

        Assert.Equal(0, result.TotalRows);
        Assert.Single(result.Errors);
        Assert.Contains("формат", result.Errors[0], StringComparison.OrdinalIgnoreCase);
    }
}
