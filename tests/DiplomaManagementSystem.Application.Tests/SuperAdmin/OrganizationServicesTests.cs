using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Tests.SuperAdmin;

public sealed class FacultyAdminServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly FacultyAdminService _service;

    public FacultyAdminServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _service = new FacultyAdminService(_dbContext);
    }

    [Fact]
    public async Task CreateAsync_WhenNameUnique_CreatesFaculty()
    {
        Guid id = await _service.CreateAsync(new FacultyFormDto(null, "Факультет ІТ"));

        Assert.NotNull(await _dbContext.Faculties.FindAsync(id));
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateName_Throws()
    {
        await _service.CreateAsync(new FacultyFormDto(null, "Факультет ІТ"));

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new FacultyFormDto(null, "Факультет ІТ")));
    }

    [Fact]
    public async Task DeactivateAsync_SetsInactive()
    {
        Guid id = await _service.CreateAsync(new FacultyFormDto(null, "Факультет ІТ"));

        await _service.DeactivateAsync(id);

        Assert.False((await _dbContext.Faculties.FindAsync(id))!.IsActive);
    }

    public void Dispose() => _dbContext.Dispose();
}

public sealed class OrganizationStructureImportServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly OrganizationStructureImportService _service;

    public OrganizationStructureImportServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _service = new OrganizationStructureImportService(_dbContext);
    }

    [Fact]
    public async Task ImportAsync_ValidJson_CreatesFacultiesAndDepartments()
    {
        const string json = """
            [
              {
                "name": "Факультет ІТ",
                "departments": [
                  {
                    "name": "Кафедра КСМ",
                    "specialtyCode": "123",
                    "specialtyName": "Комп'ютерна інженерія",
                    "studyForm": "очної форми навчання"
                  }
                ]
              }
            ]
            """;

        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(json));
        OrganizationStructureImportResultDto result =
            await _service.ImportAsync(stream, OrganizationStructureImportMode.CreateOnly);

        Assert.Equal(1, result.FacultiesCreated);
        Assert.Equal(1, result.DepartmentsCreated);
        Assert.Equal(1, await _dbContext.Faculties.CountAsync());
        Assert.Equal(1, await _dbContext.Departments.CountAsync());
    }

    [Fact]
    public async Task ImportAsync_EmptyDepartments_RecordsError()
    {
        const string json = """[ { "name": "Факультет ІТ", "departments": [] } ]""";

        using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(json));
        OrganizationStructureImportResultDto result =
            await _service.ImportAsync(stream, OrganizationStructureImportMode.CreateOnly);

        Assert.Contains(result.Errors, error => error.Contains("не містить кафедр"));
    }

    [Fact]
    public async Task ImportAsync_UpsertMode_UpdatesExistingDepartment()
    {
        const string json = """
            [
              {
                "name": "Факультет ІТ",
                "departments": [
                  { "name": "Кафедра КСМ", "specialtyCode": "123", "specialtyName": "Old", "studyForm": "очна" }
                ]
              }
            ]
            """;

        using MemoryStream firstStream = new(System.Text.Encoding.UTF8.GetBytes(json));
        await _service.ImportAsync(firstStream, OrganizationStructureImportMode.CreateOnly);

        const string updatedJson = """
            [
              {
                "name": "Факультет ІТ",
                "departments": [
                  { "name": "Кафедра КСМ", "specialtyCode": "456", "specialtyName": "New", "studyForm": "заочна" }
                ]
              }
            ]
            """;

        using MemoryStream secondStream = new(System.Text.Encoding.UTF8.GetBytes(updatedJson));
        OrganizationStructureImportResultDto result =
            await _service.ImportAsync(secondStream, OrganizationStructureImportMode.Upsert);

        Assert.Equal(1, result.DepartmentsUpdated);
        Assert.Equal("New", (await _dbContext.Departments.SingleAsync()).SpecialtyName);
    }

    public void Dispose() => _dbContext.Dispose();
}
