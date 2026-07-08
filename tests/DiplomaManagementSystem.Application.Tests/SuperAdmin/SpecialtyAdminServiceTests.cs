using DiplomaManagementSystem.Application.SuperAdmin.Specialties;
using DiplomaManagementSystem.Application.SuperAdmin.Specialties.Dtos;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Tests.SuperAdmin;

public sealed class SpecialtyAdminServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly SpecialtyAdminService _service;

    public SpecialtyAdminServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _service = new SpecialtyAdminService(_dbContext);
    }

    [Fact]
    public async Task CreateAsync_WhenCodeUnique_CreatesSpecialty()
    {
        Guid departmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext);

        Guid id = await _service.CreateAsync(new SpecialtyFormDto(null, departmentId, "456", "Комп'ютерна інженерія"));

        Specialty? specialty = await _dbContext.Specialties.FindAsync(id);
        Assert.NotNull(specialty);
        Assert.Equal("456", specialty.Code);
        Assert.True(specialty.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateCodeInDepartment_Throws()
    {
        Guid departmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext);

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new SpecialtyFormDto(null, departmentId, "123", "Інша назва")));
    }

    [Fact]
    public async Task DeactivateAsync_WhenNoStudyGroups_Deactivates()
    {
        Guid departmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext);
        Guid id = await _service.CreateAsync(new SpecialtyFormDto(null, departmentId, "456", "Нова спеціальність"));

        await _service.DeactivateAsync(id);

        Assert.False((await _dbContext.Specialties.FindAsync(id))!.IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_WhenStudyGroupsExist_Throws()
    {
        (Guid departmentId, Guid specialtyId) = await OrganizationTestData.SeedDepartmentWithSpecialtyAsync(_dbContext);
        DefenceSession session = new()
        {
            Id = Guid.NewGuid(),
            Year = 2026,
            Type = Domain.Enums.DefenceSessionType.Bachelor,
            Status = Domain.Enums.DefenceSessionStatus.Active,
            DepartmentId = departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _dbContext.DefenceSessions.Add(session);
        _dbContext.StudyGroups.Add(new StudyGroup
        {
            Id = Guid.NewGuid(),
            Name = "КН-41",
            DefenceSessionId = session.Id,
            SpecialtyId = specialtyId,
            StudyForm = "очної форми навчання",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => _service.DeactivateAsync(specialtyId));
    }

    public void Dispose() => _dbContext.Dispose();
}
