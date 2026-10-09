using DiplomaManagementSystem.Application.Admin.AnnualRoles;
using DiplomaManagementSystem.Application.Admin.AnnualRoles.Dtos;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;
using DiplomaManagementSystem.Infrastructure.Persistence.Queries;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Tests.Admin;

public sealed class AnnualRoleServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TestDepartmentContext _departmentContext;
    private readonly AnnualRoleService _service;
    private readonly Guid _departmentId;

    public AnnualRoleServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _departmentContext = new TestDepartmentContext();
        _departmentId = OrganizationTestData.SeedDepartmentAsync(_dbContext).GetAwaiter().GetResult();
        _departmentContext.CurrentDepartmentId = _departmentId;

        UserDisplayQueries userDisplayQueries = new(_dbContext);
        TestDepartmentAuthorizationService departmentAuthorization = new(_dbContext);
        CurrentDepartmentResolver resolver = new(_departmentContext, departmentAuthorization, _dbContext);
        _service = new AnnualRoleService(_dbContext, userDisplayQueries, resolver, departmentAuthorization);
    }

    [Fact]
    public async Task GetPageAsync_WhenSessionMissing_ReturnsNull()
    {
        AnnualRolesPageDto? page = await _service.GetPageAsync(Guid.NewGuid());

        Assert.Null(page);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsAllRoleSlots()
    {
        Guid sessionId = await SeedSessionAsync();
        await SeedEmployeeAsync("Петро Петренко");

        AnnualRolesPageDto? page = await _service.GetPageAsync(sessionId);

        Assert.NotNull(page);
        Assert.Equal(sessionId, page.DefenceSessionId);
        Assert.Equal(4, page.Roles.Count);
        Assert.Contains(page.Roles, slot => slot.RoleType == AnnualRoleType.DepartmentHead);
        Assert.Contains(page.Roles, slot => slot.RoleType == AnnualRoleType.ExamCommissionSecretary);
    }

    [Fact]
    public async Task GetPageAsync_ExcludesEmployeesFromOtherDepartment()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid localEmployeeId = await SeedEmployeeAsync("Локальний");
        Guid otherDepartmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext, "Ф2", "К2");
        await SeedEmployeeAsync("Чужий", "other@test.local", otherDepartmentId);

        AnnualRolesPageDto? page = await _service.GetPageAsync(sessionId);

        Assert.NotNull(page);
        Assert.Contains(page.Employees, employee => employee.Id == localEmployeeId);
        Assert.DoesNotContain(page.Employees, employee => employee.FullName == "Чужий");
    }

    [Fact]
    public async Task AssignAsync_CreatesNewAssignment()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid employeeId = await SeedEmployeeAsync("Олена Коваленко");

        await _service.AssignAsync(new AssignAnnualRoleDto(
            sessionId,
            AnnualRoleType.FormattingReviewer,
            employeeId));

        AnnualRoleAssignment? assignment = await _dbContext.AnnualRoleAssignments.SingleOrDefaultAsync(
            row => row.DefenceSessionId == sessionId && row.RoleType == AnnualRoleType.FormattingReviewer);

        Assert.NotNull(assignment);
        Assert.Equal(employeeId, assignment.EmployeeId);
    }

    [Fact]
    public async Task AssignAsync_UpdatesExistingAssignment()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid firstEmployeeId = await SeedEmployeeAsync("Петро Петренко", "petro@test.local");
        Guid secondEmployeeId = await SeedEmployeeAsync("Олена Коваленко", "olena@test.local");

        AssignAnnualRoleDto request = new(sessionId, AnnualRoleType.DepartmentHead, firstEmployeeId);
        await _service.AssignAsync(request);
        await _service.AssignAsync(request with { EmployeeId = secondEmployeeId });

        AnnualRoleAssignment assignment = await _dbContext.AnnualRoleAssignments.SingleAsync(
            row => row.DefenceSessionId == sessionId && row.RoleType == AnnualRoleType.DepartmentHead);

        Assert.Equal(secondEmployeeId, assignment.EmployeeId);
    }

    [Fact]
    public async Task AssignAsync_WhenSessionMissing_Throws()
    {
        Guid employeeId = await SeedEmployeeAsync("Петро Петренко");

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.AssignAsync(new AssignAnnualRoleDto(
                Guid.NewGuid(),
                AnnualRoleType.AntiPlagiarismOfficer,
                employeeId)));
    }

    [Fact]
    public async Task AssignAsync_WhenEmployeeFromOtherDepartment_Throws()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid otherDepartmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext, "Ф2", "К2");
        Guid otherEmployeeId = await SeedEmployeeAsync("Чужий", "other@test.local", otherDepartmentId);

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.AssignAsync(new AssignAnnualRoleDto(
                sessionId,
                AnnualRoleType.AntiPlagiarismOfficer,
                otherEmployeeId)));
    }

    [Fact]
    public async Task AssignAsync_WhenEmployeeMissing_Throws()
    {
        Guid sessionId = await SeedSessionAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.AssignAsync(new AssignAnnualRoleDto(
                sessionId,
                AnnualRoleType.AntiPlagiarismOfficer,
                Guid.NewGuid())));
    }

    [Fact]
    public async Task GetPageAsync_ShowsAssignedEmployeeName()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid employeeId = await SeedEmployeeAsync("Петро Петренко");
        await _service.AssignAsync(new AssignAnnualRoleDto(
            sessionId,
            AnnualRoleType.ExamCommissionSecretary,
            employeeId));

        AnnualRolesPageDto? page = await _service.GetPageAsync(sessionId);

        Assert.NotNull(page);
        AnnualRoleSlotDto slot = Assert.Single(page.Roles, role => role.RoleType == AnnualRoleType.ExamCommissionSecretary);
        Assert.Equal(employeeId, slot.AssignedEmployeeId);
        Assert.Equal("Петро Петренко", slot.AssignedEmployeeName);
    }

    private async Task<Guid> SeedSessionAsync()
    {
        var sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Semester = 2,
            Status = DefenceSessionStatus.Active,
            DepartmentId = _departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return sessionId;
    }

    private async Task<Guid> SeedEmployeeAsync(
        string fullName,
        string? email = null,
        Guid? departmentId = null)
    {
        var employeeId = Guid.NewGuid();
        string resolvedEmail = email ?? $"{employeeId:N}@test.local";
        Guid targetDepartmentId = departmentId ?? _departmentId;
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = employeeId,
            Email = resolvedEmail,
            UserName = resolvedEmail,
            FullName = fullName,
            UserKind = UserKind.Employee,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        _dbContext.DepartmentEmployees.Add(new DepartmentEmployee
        {
            Id = Guid.NewGuid(),
            DepartmentId = targetDepartmentId,
            UserId = employeeId,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return employeeId;
    }

    public void Dispose() => _dbContext.Dispose();

    private sealed class TestDepartmentAuthorizationService(ApplicationDbContext dbContext)
        : IDepartmentAuthorizationService
    {
        public Task<bool> IsSuperAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<Guid>> GetAdminDepartmentIdsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> GetEmployeeDepartmentIdsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task EnsureDepartmentAdminAccessAsync(
            Guid userId,
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task EnsureDepartmentEmployeeAccessAsync(
            Guid userId,
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task EnsureSessionInDepartmentAsync(
            Guid sessionId,
            Guid departmentId,
            CancellationToken cancellationToken = default)
        {
            bool belongs = await dbContext.DefenceSessions
                .AsNoTracking()
                .AnyAsync(
                    session => session.Id == sessionId && session.DepartmentId == departmentId,
                    cancellationToken);

            if (!belongs)
            {
                throw new DomainException(DepartmentMessages.SessionNotInDepartment);
            }
        }
    }
}
