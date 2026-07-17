using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.SuperAdmin.Specialties;
using DiplomaManagementSystem.Application.SuperAdmin.Specialties.Dtos;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Tests.SuperAdmin;

public sealed class SpecialtyAdminServiceCoverageTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SpecialtyAdminService _service;

    public SpecialtyAdminServiceCoverageTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _service = new SpecialtyAdminService(_dbContext);
    }

    [Fact]
    public async Task GetByDepartmentAsync_ReturnsSpecialties()
    {
        (Guid departmentId, Guid specialtyId) = await OrganizationTestData.SeedDepartmentWithSpecialtyAsync(_dbContext);

        IReadOnlyList<SpecialtyListItemDto> items = await _service.GetByDepartmentAsync(departmentId);

        Assert.Contains(items, item => item.Id == specialtyId && item.IsActive);
    }

    [Fact]
    public async Task GetActiveOptionsForDepartmentAsync_ReturnsOnlyActive()
    {
        Guid departmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext);
        Guid activeId = await _service.CreateAsync(new SpecialtyFormDto(null, departmentId, "456", "Активна"));
        Guid inactiveId = await _service.CreateAsync(new SpecialtyFormDto(null, departmentId, "789", "Неактивна"));
        await _service.DeactivateAsync(inactiveId);

        IReadOnlyList<SpecialtyOptionDto> options = await _service.GetActiveOptionsForDepartmentAsync(departmentId);

        Assert.Contains(options, option => option.Id == activeId);
        Assert.DoesNotContain(options, option => option.Id == inactiveId);
    }

    [Fact]
    public async Task GetActiveOptionsForSessionAsync_ReturnsDepartmentSpecialties()
    {
        (Guid departmentId, Guid specialtyId) = await OrganizationTestData.SeedDepartmentWithSpecialtyAsync(_dbContext);
        Guid sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Status = DefenceSessionStatus.Active,
            DepartmentId = departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        IReadOnlyList<SpecialtyOptionDto> options = await _service.GetActiveOptionsForSessionAsync(sessionId);

        Assert.Contains(options, option => option.Id == specialtyId);
    }

    [Fact]
    public async Task UpdateAsync_ChangesCodeAndName()
    {
        Guid departmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext);
        Guid id = await _service.CreateAsync(new SpecialtyFormDto(null, departmentId, "456", "Стара"));

        await _service.UpdateAsync(id, new SpecialtyFormDto(id, departmentId, "777", "Нова"));

        Specialty specialty = (await _dbContext.Specialties.FindAsync(id))!;
        Assert.Equal("777", specialty.Code);
        Assert.Equal("Нова", specialty.Name);
    }

    [Fact]
    public async Task UpdateAsync_WhenDepartmentChanged_Throws()
    {
        Guid departmentA = await OrganizationTestData.SeedDepartmentAsync(_dbContext, "Ф1", "К1");
        Guid departmentB = await OrganizationTestData.SeedDepartmentAsync(_dbContext, "Ф2", "К2");
        Guid id = await _service.CreateAsync(new SpecialtyFormDto(null, departmentA, "456", "С"));

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.UpdateAsync(id, new SpecialtyFormDto(id, departmentB, "456", "С")));
    }

    public void Dispose() => _dbContext.Dispose();
}

public sealed class DefaultDepartmentResolverTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DefaultDepartmentResolver _resolver;

    public DefaultDepartmentResolverTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _resolver = new DefaultDepartmentResolver(_dbContext);
    }

    [Fact]
    public async Task ResolveRequiredForNewSessionAsync_WhenSingleDepartment_ReturnsId()
    {
        Guid departmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext);

        Guid resolved = await _resolver.ResolveRequiredForNewSessionAsync();

        Assert.Equal(departmentId, resolved);
    }

    [Fact]
    public async Task ResolveRequiredForNewSessionAsync_WhenMultipleDepartments_Throws()
    {
        await OrganizationTestData.SeedDepartmentAsync(_dbContext, "Ф1", "К1");
        await OrganizationTestData.SeedDepartmentAsync(_dbContext, "Ф2", "К2");

        await Assert.ThrowsAsync<DomainException>(() =>
            _resolver.ResolveRequiredForNewSessionAsync());
    }

    public void Dispose() => _dbContext.Dispose();
}

public sealed class CourseUkrainianLabelTests
{
    [Theory]
    [InlineData(1, "першого")]
    [InlineData(2, "другого")]
    [InlineData(3, "третього")]
    [InlineData(4, "четвертого")]
    [InlineData(5, "п'ятого")]
    [InlineData(6, "шостого")]
    [InlineData(7, "7-го")]
    public void FormatGenitive_ReturnsExpected(int course, string expected)
    {
        Assert.Equal(expected, CourseUkrainianLabel.FormatGenitive(course));
    }
}
