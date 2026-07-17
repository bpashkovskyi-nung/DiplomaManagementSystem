using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Application.Tests.Departments;

public sealed class DepartmentAuthorizationServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly DepartmentAuthorizationService _service;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly Guid _departmentId;

    public DepartmentAuthorizationServiceTests()
    {
        string databaseName = Guid.NewGuid().ToString();
        ServiceCollection services = new();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<DepartmentAuthorizationService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _service = _serviceProvider.GetRequiredService<DepartmentAuthorizationService>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        _departmentId = OrganizationTestData.SeedDepartmentAsync(_dbContext).GetAwaiter().GetResult();
    }

    [Fact]
    public async Task IsSuperAdminAsync_WhenUserHasRole_ReturnsTrue()
    {
        await EnsureRoleAsync(RoleNames.SuperAdmin);
        ApplicationUser user = await CreateUserAsync("super@test.local");
        await _userManager.AddToRoleAsync(user, RoleNames.SuperAdmin);

        bool isSuperAdmin = await _service.IsSuperAdminAsync(user.Id);

        Assert.True(isSuperAdmin);
    }

    [Fact]
    public async Task GetAdminDepartmentIdsAsync_ReturnsAssignments()
    {
        ApplicationUser admin = await CreateUserAsync("admin@test.local");
        _dbContext.DepartmentAdminAssignments.Add(new DepartmentAdminAssignment
        {
            Id = Guid.NewGuid(),
            DepartmentId = _departmentId,
            UserId = admin.Id,
            AssignedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        IReadOnlyList<Guid> departmentIds = await _service.GetAdminDepartmentIdsAsync(admin.Id);

        Assert.Contains(_departmentId, departmentIds);
    }

    [Fact]
    public async Task GetEmployeeDepartmentIdsAsync_ReturnsOnlyActiveMemberships()
    {
        ApplicationUser employee = await CreateUserAsync("employee@test.local", UserKind.Employee);
        _dbContext.DepartmentEmployees.AddRange(
            new DepartmentEmployee
            {
                Id = Guid.NewGuid(),
                DepartmentId = _departmentId,
                UserId = employee.Id,
                FullName = employee.FullName,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
            },
            new DepartmentEmployee
            {
                Id = Guid.NewGuid(),
                DepartmentId = Guid.NewGuid(),
                UserId = employee.Id,
                FullName = employee.FullName,
                IsActive = false,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        await _dbContext.SaveChangesAsync();

        IReadOnlyList<Guid> departmentIds = await _service.GetEmployeeDepartmentIdsAsync(employee.Id);

        Assert.Equal([_departmentId], departmentIds);
    }

    [Fact]
    public async Task EnsureDepartmentAdminAccessAsync_WhenSuperAdmin_Allows()
    {
        await EnsureRoleAsync(RoleNames.SuperAdmin);
        ApplicationUser user = await CreateUserAsync("super@test.local");
        await _userManager.AddToRoleAsync(user, RoleNames.SuperAdmin);

        await _service.EnsureDepartmentAdminAccessAsync(user.Id, _departmentId);
    }

    [Fact]
    public async Task EnsureDepartmentAdminAccessAsync_WhenNotAssigned_Throws()
    {
        ApplicationUser user = await CreateUserAsync("outsider@test.local");

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.EnsureDepartmentAdminAccessAsync(user.Id, _departmentId));
    }

    [Fact]
    public async Task EnsureDepartmentEmployeeAccessAsync_WhenNotMember_Throws()
    {
        ApplicationUser user = await CreateUserAsync("outsider@test.local", UserKind.Employee);

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.EnsureDepartmentEmployeeAccessAsync(user.Id, _departmentId));
    }

    [Fact]
    public async Task EnsureSessionInDepartmentAsync_WhenSessionBelongs_Allows()
    {
        Guid sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Status = DefenceSessionStatus.Active,
            DepartmentId = _departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        await _service.EnsureSessionInDepartmentAsync(sessionId, _departmentId);
    }

    [Fact]
    public async Task EnsureSessionInDepartmentAsync_WhenSessionInOtherDepartment_Throws()
    {
        Guid sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Status = DefenceSessionStatus.Active,
            DepartmentId = _departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.EnsureSessionInDepartmentAsync(sessionId, Guid.NewGuid()));
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        if (!await _roleManager.RoleExistsAsync(roleName))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
            });
        }
    }

    private async Task<ApplicationUser> CreateUserAsync(string email, UserKind userKind = UserKind.Employee)
    {
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = "Test User",
            UserKind = userKind,
            CreatedAt = DateTimeOffset.UtcNow,
            EmailConfirmed = true,
        };

        await _userManager.CreateAsync(user);
        return user;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}
