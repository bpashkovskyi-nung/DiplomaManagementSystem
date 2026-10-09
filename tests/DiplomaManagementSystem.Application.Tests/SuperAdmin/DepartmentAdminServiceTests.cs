using DiplomaManagementSystem.Application.SuperAdmin.Departments;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Tests.SuperAdmin;

public sealed class DepartmentAdminServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DepartmentAdminService _service;
    private readonly FacultyAdminService _facultyService;

    public DepartmentAdminServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _service = new DepartmentAdminService(_dbContext);
        _facultyService = new FacultyAdminService(_dbContext);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_CreatesDepartment()
    {
        Guid facultyId = await _facultyService.CreateAsync(new FacultyFormDto(null, "Факультет ІТ"));

        Guid departmentId = await _service.CreateAsync(new DepartmentFormDto(null, facultyId, "Кафедра КСМ"));

        Assert.NotNull(await _dbContext.Departments.FindAsync(departmentId));
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateNameInFaculty_Throws()
    {
        Guid facultyId = await _facultyService.CreateAsync(new FacultyFormDto(null, "Факультет ІТ"));
        await _service.CreateAsync(new DepartmentFormDto(null, facultyId, "Кафедра КСМ"));

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new DepartmentFormDto(null, facultyId, "Кафедра КСМ")));
    }

    [Fact]
    public async Task CreateAsync_WhenFacultyMissing_Throws()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new DepartmentFormDto(null, Guid.NewGuid(), "Кафедра")));
    }

    [Fact]
    public async Task GetAllAsync_WhenFilteredByFaculty_ReturnsOnlyFacultyDepartments()
    {
        Guid facultyA = await _facultyService.CreateAsync(new FacultyFormDto(null, "Факультет A"));
        Guid facultyB = await _facultyService.CreateAsync(new FacultyFormDto(null, "Факультет B"));
        await _service.CreateAsync(new DepartmentFormDto(null, facultyA, "Кафедра A"));
        await _service.CreateAsync(new DepartmentFormDto(null, facultyB, "Кафедра B"));

        IReadOnlyList<DepartmentListItemDto> items = await _service.GetAllAsync(facultyA);

        DepartmentListItemDto item = Assert.Single(items);
        Assert.Equal("Кафедра A", item.Name);
        Assert.Equal(facultyA, item.FacultyId);
    }

    [Fact]
    public async Task GetForEditAsync_WhenExists_ReturnsForm()
    {
        Guid facultyId = await _facultyService.CreateAsync(new FacultyFormDto(null, "Факультет ІТ"));
        Guid departmentId = await _service.CreateAsync(new DepartmentFormDto(null, facultyId, "Кафедра КСМ"));

        DepartmentFormDto? form = await _service.GetForEditAsync(departmentId);

        Assert.NotNull(form);
        Assert.Equal(departmentId, form.Id);
        Assert.Equal("Кафедра КСМ", form.Name);
    }

    [Fact]
    public async Task GetForEditAsync_WhenMissing_ReturnsNull()
    {
        DepartmentFormDto? form = await _service.GetForEditAsync(Guid.NewGuid());

        Assert.Null(form);
    }

    [Fact]
    public async Task UpdateAsync_ChangesNameAndFaculty()
    {
        Guid facultyA = await _facultyService.CreateAsync(new FacultyFormDto(null, "Факультет A"));
        Guid facultyB = await _facultyService.CreateAsync(new FacultyFormDto(null, "Факультет B"));
        Guid departmentId = await _service.CreateAsync(new DepartmentFormDto(null, facultyA, "Кафедра стара"));

        await _service.UpdateAsync(departmentId, new DepartmentFormDto(departmentId, facultyB, "Кафедра нова"));

        Department? department = await _dbContext.Departments.FindAsync(departmentId);
        Assert.NotNull(department);
        Assert.Equal(facultyB, department.FacultyId);
        Assert.Equal("Кафедра нова", department.Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_Throws()
    {
        Guid facultyId = await _facultyService.CreateAsync(new FacultyFormDto(null, "Факультет"));

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.UpdateAsync(Guid.NewGuid(), new DepartmentFormDto(Guid.NewGuid(), facultyId, "Кафедра")));
    }

    [Fact]
    public async Task DeactivateAsync_SetsInactive()
    {
        Guid facultyId = await _facultyService.CreateAsync(new FacultyFormDto(null, "Факультет"));
        Guid departmentId = await _service.CreateAsync(new DepartmentFormDto(null, facultyId, "Кафедра"));

        await _service.DeactivateAsync(departmentId);

        Assert.False((await _dbContext.Departments.FindAsync(departmentId))!.IsActive);
    }

    [Fact]
    public async Task GetAllAsync_CountsActiveSpecialties()
    {
        Guid departmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext);

        IReadOnlyList<DepartmentListItemDto> items = await _service.GetAllAsync();

        DepartmentListItemDto item = Assert.Single(items);
        Assert.Equal(departmentId, item.Id);
        Assert.Equal(1, item.SpecialtyCount);
    }

    public void Dispose() => _dbContext.Dispose();
}
