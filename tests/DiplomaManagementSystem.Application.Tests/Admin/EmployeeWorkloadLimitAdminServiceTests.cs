using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;
using DiplomaManagementSystem.Application.Identity;
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
    private readonly EmployeeWorkloadLimitAdminService _service;

    public EmployeeWorkloadLimitAdminServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _service = new EmployeeWorkloadLimitAdminService(_dbContext, new EmployeeWorkloadLimitQueries(_dbContext));
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
    public async Task SetLimitAsync_WhenEmployeeMissing_Throws()
    {
        Guid sessionId = await SeedSessionAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.SetLimitAsync(new SetEmployeeWorkloadLimitDto(sessionId, Guid.NewGuid(), 1, null)));
    }

    private async Task<Guid> SeedSessionAsync()
    {
        Guid sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Semester = 1,
            Status = DefenceSessionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return sessionId;
    }

    private async Task<Guid> SeedEmployeeAsync(string fullName)
    {
        Guid employeeId = Guid.NewGuid();
        string email = $"{employeeId:N}@test.local";
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = employeeId,
            Email = email,
            UserName = email,
            FullName = fullName,
            UserKind = UserKind.Employee,
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
        Guid studentId = Guid.NewGuid();
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

        Guid diplomaId = Guid.NewGuid();
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

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
