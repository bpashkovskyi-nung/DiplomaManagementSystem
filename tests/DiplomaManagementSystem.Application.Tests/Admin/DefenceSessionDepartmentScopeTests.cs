using DiplomaManagementSystem.Application.Admin.DefenceSessions;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Application.Audit.Contracts;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;
using DiplomaManagementSystem.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Tests.Admin;

public sealed class DefenceSessionDepartmentScopeTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TestDepartmentContext _departmentContext;
    private readonly DefenceSessionService _service;
    private readonly Guid _departmentA;
    private readonly Guid _departmentB;

    public DefenceSessionDepartmentScopeTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _departmentContext = new TestDepartmentContext();
        TestDepartmentAuthorizationService departmentAuthorization = new(_dbContext);
        CurrentDepartmentResolver resolver = new(
            _departmentContext,
            departmentAuthorization,
            _dbContext);

        _service = new DefenceSessionService(
            _dbContext,
            new DefenceSessionArchiveService(),
            new NoOpAuditLogWriter(),
            resolver,
            departmentAuthorization);

        _departmentA = OrganizationTestData.SeedDepartmentAsync(_dbContext, "Факультет A", "Кафедра A").GetAwaiter().GetResult();
        _departmentB = OrganizationTestData.SeedDepartmentAsync(_dbContext, "Факультет B", "Кафедра B").GetAwaiter().GetResult();
        SeedSession(_departmentA);
        SeedSession(_departmentB);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyCurrentDepartmentSessions()
    {
        _departmentContext.CurrentDepartmentId = _departmentA;

        IReadOnlyList<DefenceSessionListItemDto> items = await _service.GetAllAsync();

        Assert.Single(items);
    }

    [Fact]
    public async Task CreateAsync_SetsDepartmentIdFromContext()
    {
        _departmentContext.CurrentDepartmentId = _departmentB;

        Guid id = await _service.CreateAsync(new DefenceSessionFormDto(null, 2027, DefenceSessionType.Master, 1));

        DefenceSession? session = await _dbContext.DefenceSessions.FindAsync(id);
        Assert.Equal(_departmentB, session!.DepartmentId);
    }

    public void Dispose() => _dbContext.Dispose();

    private void SeedSession(Guid departmentId)
    {
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = Guid.NewGuid(),
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Status = DefenceSessionStatus.Active,
            DepartmentId = departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        _dbContext.SaveChanges();
    }

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

    private sealed class NoOpAuditLogWriter : IAuditLogWriter
    {
        public Task WriteAsync(AuditLogEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
