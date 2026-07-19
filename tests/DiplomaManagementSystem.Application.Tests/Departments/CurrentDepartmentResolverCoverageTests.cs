using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Tests.Departments;

public sealed class CurrentDepartmentResolverCoverageTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TestDepartmentContext _departmentContext;
    private readonly ConfigurableDepartmentAuthorizationService _authorization;
    private readonly CurrentDepartmentResolver _resolver;
    private readonly Guid _departmentA;
    private readonly Guid _departmentB;
    private readonly Guid _userId = Guid.NewGuid();

    public CurrentDepartmentResolverCoverageTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _departmentContext = new TestDepartmentContext();
        _authorization = new ConfigurableDepartmentAuthorizationService();
        _resolver = new CurrentDepartmentResolver(_departmentContext, _authorization, _dbContext);

        _departmentA = OrganizationTestData.SeedDepartmentAsync(_dbContext, "Факультет A", "Кафедра A").GetAwaiter().GetResult();
        _departmentB = OrganizationTestData.SeedDepartmentAsync(_dbContext, "Факультет B", "Кафедра B").GetAwaiter().GetResult();
    }

    [Fact]
    public async Task ResolveRequiredAdminDepartmentIdAsync_FromContext_ReturnsContext()
    {
        _departmentContext.CurrentDepartmentId = _departmentA;

        Guid resolved = await _resolver.ResolveRequiredAdminDepartmentIdAsync(_userId);

        Assert.Equal(_departmentA, resolved);
    }

    [Fact]
    public async Task ResolveRequiredAdminDepartmentIdAsync_SingleAssignment_ReturnsAssignment()
    {
        _authorization.SetAdminDepartments(_userId, _departmentA);

        Guid resolved = await _resolver.ResolveRequiredAdminDepartmentIdAsync(_userId);

        Assert.Equal(_departmentA, resolved);
    }

    [Fact]
    public async Task ResolveRequiredAdminDepartmentIdAsync_SuperAdminWithSingleDepartment_ReturnsDepartment()
    {
        await DeactivateDepartmentAsync(_departmentB);
        _authorization.IsSuperAdmin = true;

        Guid resolved = await _resolver.ResolveRequiredAdminDepartmentIdAsync(_userId);

        Assert.Equal(_departmentA, resolved);
    }

    [Fact]
    public async Task ResolveRequiredAdminDepartmentIdAsync_MultipleAssignments_Throws()
    {
        _authorization.SetAdminDepartments(_userId, _departmentA, _departmentB);

        await Assert.ThrowsAsync<DomainException>(() =>
            _resolver.ResolveRequiredAdminDepartmentIdAsync(_userId));
    }

    [Fact]
    public async Task ResolveRequiredEmployeeDepartmentIdAsync_FromContext_ReturnsContext()
    {
        _departmentContext.CurrentDepartmentId = _departmentA;

        Guid resolved = await _resolver.ResolveRequiredEmployeeDepartmentIdAsync(_userId);

        Assert.Equal(_departmentA, resolved);
    }

    [Fact]
    public async Task ResolveRequiredEmployeeDepartmentIdAsync_SingleMembership_ReturnsDepartment()
    {
        _authorization.SetEmployeeDepartments(_userId, _departmentA);

        Guid resolved = await _resolver.ResolveRequiredEmployeeDepartmentIdAsync(_userId);

        Assert.Equal(_departmentA, resolved);
    }

    [Fact]
    public async Task ResolveRequiredEmployeeDepartmentIdAsync_MultipleMemberships_Throws()
    {
        _authorization.SetEmployeeDepartments(_userId, _departmentA, _departmentB);

        await Assert.ThrowsAsync<DomainException>(() =>
            _resolver.ResolveRequiredEmployeeDepartmentIdAsync(_userId));
    }

    [Fact]
    public async Task ResolveRequiredScopedDepartmentIdAsync_FromContext_ReturnsContext()
    {
        _departmentContext.CurrentDepartmentId = _departmentB;

        Guid resolved = await _resolver.ResolveRequiredScopedDepartmentIdAsync();

        Assert.Equal(_departmentB, resolved);
    }

    [Fact]
    public async Task TryResolveScopedDepartmentIdAsync_WhenSingleActiveDepartment_ReturnsId()
    {
        await DeactivateDepartmentAsync(_departmentB);

        Guid? resolved = await _resolver.TryResolveScopedDepartmentIdAsync();

        Assert.Equal(_departmentA, resolved);
    }

    [Fact]
    public async Task ResolveRequiredScopedDepartmentIdAsync_WhenMultipleDepartments_Throws()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            _resolver.ResolveRequiredScopedDepartmentIdAsync());
    }

    private async Task DeactivateDepartmentAsync(Guid departmentId)
    {
        Department? department = await _dbContext.Departments.FindAsync(departmentId);
        department!.IsActive = false;
        await _dbContext.SaveChangesAsync();
    }

    public void Dispose() => _dbContext.Dispose();

    private sealed class ConfigurableDepartmentAuthorizationService : IDepartmentAuthorizationService
    {
        private readonly Dictionary<Guid, List<Guid>> _adminDepartments = [];
        private readonly Dictionary<Guid, List<Guid>> _employeeDepartments = [];

        public bool IsSuperAdmin { get; set; }

        public void SetAdminDepartments(Guid userId, params Guid[] departmentIds) =>
            _adminDepartments[userId] = departmentIds.ToList();

        public void SetEmployeeDepartments(Guid userId, params Guid[] departmentIds) =>
            _employeeDepartments[userId] = departmentIds.ToList();

        public Task<bool> IsSuperAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(IsSuperAdmin);

        public Task<IReadOnlyList<Guid>> GetAdminDepartmentIdsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(_adminDepartments.GetValueOrDefault(userId, []));

        public Task<IReadOnlyList<Guid>> GetEmployeeDepartmentIdsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>(_employeeDepartments.GetValueOrDefault(userId, []));

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

        public Task EnsureSessionInDepartmentAsync(
            Guid sessionId,
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
