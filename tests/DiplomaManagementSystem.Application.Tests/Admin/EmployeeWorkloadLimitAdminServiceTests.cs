using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;
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

public sealed class EmployeeWorkloadLimitAdminServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TestDepartmentContext _departmentContext;
    private readonly EmployeeWorkloadLimitAdminService _service;
    private readonly Guid _departmentId;

    public EmployeeWorkloadLimitAdminServiceTests()
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
        _service = new EmployeeWorkloadLimitAdminService(
            _dbContext,
            new EmployeeWorkloadLimitQueries(_dbContext),
            userDisplayQueries,
            resolver,
            departmentAuthorization);
    }

    [Fact]
    public async Task GetPageAsync_WhenSessionMissing_ReturnsNull()
    {
        EmployeeWorkloadLimitsPageDto? page = await _service.GetPageAsync(Guid.NewGuid());

        Assert.Null(page);
    }

    [Fact]
    public async Task GetPageAsync_ReturnsEmployeesWithCountsAndLimits()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid employeeId = await SeedEmployeeAsync("Петро Петренко");
        await SeedDiplomaAsync(sessionId, employeeId, supervisorConfirmed: true, reviewerId: employeeId);

        await _service.SetLimitAsync(new SetEmployeeWorkloadLimitDto(sessionId, employeeId, 5, 3));

        EmployeeWorkloadLimitsPageDto? page = await _service.GetPageAsync(sessionId);

        Assert.NotNull(page);
        EmployeeWorkloadLimitRowDto row = Assert.Single(page.Rows, item => item.EmployeeId == employeeId);
        Assert.Equal(5, row.MaxSupervisorStudents);
        Assert.Equal(3, row.MaxReviewerStudents);
        Assert.Equal(1, row.ConfirmedSupervisorCount);
        Assert.Equal(1, row.ReviewerAssignmentCount);
    }

    [Fact]
    public async Task GetPageAsync_ExcludesEmployeesFromOtherDepartment()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid localEmployeeId = await SeedEmployeeAsync("Локальний");
        Guid otherDepartmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext, "Ф2", "К2");
        await SeedEmployeeAsync("Чужий", otherDepartmentId);

        EmployeeWorkloadLimitsPageDto? page = await _service.GetPageAsync(sessionId);

        Assert.NotNull(page);
        Assert.Contains(page.Rows, row => row.EmployeeId == localEmployeeId);
        Assert.DoesNotContain(page.Rows, row => row.FullName == "Чужий");
    }

    [Fact]
    public async Task SetLimitAsync_CreatesAndUpdatesRow()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid employeeId = await SeedEmployeeAsync("Олена Коваленко");

        await _service.SetLimitAsync(new SetEmployeeWorkloadLimitDto(sessionId, employeeId, 2, 1));
        await _service.SetLimitAsync(new SetEmployeeWorkloadLimitDto(sessionId, employeeId, 4, 2));

        EmployeeSessionWorkloadLimit? limit = await _dbContext.EmployeeSessionWorkloadLimits.SingleOrDefaultAsync(
            row => row.DefenceSessionId == sessionId && row.EmployeeId == employeeId);

        Assert.NotNull(limit);
        Assert.Equal(4, limit.MaxSupervisorStudents);
        Assert.Equal(2, limit.MaxReviewerStudents);
    }

    [Fact]
    public async Task SetLimitAsync_WhenBothNull_RemovesRow()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid employeeId = await SeedEmployeeAsync("Іван Коваль");

        await _service.SetLimitAsync(new SetEmployeeWorkloadLimitDto(sessionId, employeeId, 1, 1));
        await _service.SetLimitAsync(new SetEmployeeWorkloadLimitDto(sessionId, employeeId, null, null));

        Assert.False(await _dbContext.EmployeeSessionWorkloadLimits.AnyAsync(
            row => row.DefenceSessionId == sessionId && row.EmployeeId == employeeId));
    }

    [Fact]
    public async Task SetLimitAsync_WhenSessionMissing_Throws()
    {
        Guid employeeId = await SeedEmployeeAsync("Петро Петренко");

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.SetLimitAsync(new SetEmployeeWorkloadLimitDto(Guid.NewGuid(), employeeId, 1, null)));
    }

    [Fact]
    public async Task SetLimitAsync_WhenEmployeeFromOtherDepartment_Throws()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid otherDepartmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext, "Ф2", "К2");
        Guid otherEmployeeId = await SeedEmployeeAsync("Чужий", otherDepartmentId);

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.SetLimitAsync(new SetEmployeeWorkloadLimitDto(sessionId, otherEmployeeId, 1, null)));
    }

    [Fact]
    public async Task SetLimitAsync_WhenEmployeeMissing_Throws()
    {
        Guid sessionId = await SeedSessionAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.SetLimitAsync(new SetEmployeeWorkloadLimitDto(sessionId, Guid.NewGuid(), 1, null)));
    }

    private async Task<Guid> SeedSessionAsync()
    {
        var sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Semester = 1,
            Status = DefenceSessionStatus.Active,
            DepartmentId = _departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return sessionId;
    }

    private async Task<Guid> SeedEmployeeAsync(string fullName, Guid? departmentId = null)
    {
        var employeeId = Guid.NewGuid();
        string email = $"{employeeId:N}@test.local";
        Guid targetDepartmentId = departmentId ?? _departmentId;
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = employeeId,
            Email = email,
            UserName = email,
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

    private async Task<Guid> SeedDiplomaAsync(
        Guid sessionId,
        Guid employeeId,
        bool supervisorConfirmed,
        Guid? reviewerId)
    {
        var studentId = Guid.NewGuid();
        string email = $"{studentId:N}@student.test";
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = studentId,
            Email = email,
            UserName = email,
            FullName = "Student",
            UserKind = UserKind.Student,
            DefenceSessionId = sessionId,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        var diplomaId = Guid.NewGuid();
        _dbContext.Diplomas.Add(new Diploma
        {
            Id = diplomaId,
            DefenceSessionId = sessionId,
            StudentId = studentId,
            SupervisorId = employeeId,
            SupervisorAssignmentStatus = supervisorConfirmed
                ? SupervisorAssignmentStatus.Confirmed
                : SupervisorAssignmentStatus.Pending,
            ReviewerId = reviewerId,
            ReviewAssignmentStatus = reviewerId.HasValue
                ? ReviewAssignmentStatus.Assigned
                : ReviewAssignmentStatus.NotAssigned,
            LifecycleStatus = DiplomaLifecycleStatus.TopicApproved,
            AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return diplomaId;
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
