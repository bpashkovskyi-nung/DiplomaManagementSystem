using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Infrastructure.Persistence;
using DiplomaManagementSystem.Infrastructure.Persistence.Queries;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Tests.Infrastructure;

public sealed class EmployeeWorkloadLimitQueriesTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly EmployeeWorkloadLimitQueries _queries;

    public EmployeeWorkloadLimitQueriesTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _queries = new EmployeeWorkloadLimitQueries(_dbContext);
    }

    [Fact]
    public async Task GetSupervisorLimitAsync_WhenMissing_ReturnsNull()
    {
        Guid sessionId = await SeedSessionAsync();
        var employeeId = Guid.NewGuid();

        int? limit = await _queries.GetSupervisorLimitAsync(sessionId, employeeId);

        Assert.Null(limit);
    }

    [Fact]
    public async Task CountConfirmedSupervisorStudentsAsync_CountsOnlyConfirmed()
    {
        Guid sessionId = await SeedSessionAsync();
        var supervisorId = Guid.NewGuid();
        await SeedDiplomaAsync(sessionId, supervisorId, SupervisorAssignmentStatus.Confirmed);
        await SeedDiplomaAsync(sessionId, supervisorId, SupervisorAssignmentStatus.Pending);
        await SeedDiplomaAsync(sessionId, supervisorId, SupervisorAssignmentStatus.Rejected);

        int count = await _queries.CountConfirmedSupervisorStudentsAsync(sessionId, supervisorId);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CountConfirmedSupervisorStudentsAsync_ExcludesDiploma()
    {
        Guid sessionId = await SeedSessionAsync();
        var supervisorId = Guid.NewGuid();
        Guid excludedDiplomaId = await SeedDiplomaAsync(sessionId, supervisorId, SupervisorAssignmentStatus.Confirmed);
        await SeedDiplomaAsync(sessionId, supervisorId, SupervisorAssignmentStatus.Confirmed);

        int count = await _queries.CountConfirmedSupervisorStudentsAsync(
            sessionId,
            supervisorId,
            excludedDiplomaId);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CountReviewerAssignmentsAsync_CountsAllAssignedReviewers()
    {
        Guid sessionId = await SeedSessionAsync();
        var reviewerId = Guid.NewGuid();
        await SeedDiplomaWithReviewerAsync(sessionId, reviewerId, ReviewAssignmentStatus.Assigned);
        await SeedDiplomaWithReviewerAsync(sessionId, reviewerId, ReviewAssignmentStatus.Completed);

        int count = await _queries.CountReviewerAssignmentsAsync(sessionId, reviewerId);

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CountReviewerAssignmentsAsync_ExcludesDiploma()
    {
        Guid sessionId = await SeedSessionAsync();
        var reviewerId = Guid.NewGuid();
        Guid excludedDiplomaId = await SeedDiplomaWithReviewerAsync(sessionId, reviewerId, ReviewAssignmentStatus.Assigned);
        await SeedDiplomaWithReviewerAsync(sessionId, reviewerId, ReviewAssignmentStatus.Assigned);

        int count = await _queries.CountReviewerAssignmentsAsync(sessionId, reviewerId, excludedDiplomaId);

        Assert.Equal(1, count);
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
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return sessionId;
    }

    private async Task<Guid> SeedDiplomaAsync(
        Guid sessionId,
        Guid supervisorId,
        SupervisorAssignmentStatus status)
    {
        var studentId = Guid.NewGuid();
        string email = $"{Guid.NewGuid():N}@student.test";
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
            SupervisorId = supervisorId,
            SupervisorAssignmentStatus = status,
            LifecycleStatus = DiplomaLifecycleStatus.AwaitingSupervisor,
            AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return diplomaId;
    }

    private async Task<Guid> SeedDiplomaWithReviewerAsync(
        Guid sessionId,
        Guid reviewerId,
        ReviewAssignmentStatus status)
    {
        var studentId = Guid.NewGuid();
        string email = $"{Guid.NewGuid():N}@student.test";
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
            ReviewerId = reviewerId,
            ReviewAssignmentStatus = status,
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
