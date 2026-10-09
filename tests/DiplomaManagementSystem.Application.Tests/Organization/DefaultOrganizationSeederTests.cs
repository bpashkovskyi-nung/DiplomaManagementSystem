using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Infrastructure.Persistence;
using DiplomaManagementSystem.Infrastructure.Persistence.Seeding;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace DiplomaManagementSystem.Application.Tests.Organization;

public sealed class DefaultOrganizationSeederTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public DefaultOrganizationSeederTests()
    {
        string databaseName = Guid.NewGuid().ToString();
        ServiceCollection services = new();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    }

    [Fact]
    public async Task EnsureAsync_WhenDatabaseEmpty_SeedsOrganizationAndBackfillsSession()
    {
        var sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Status = DefenceSessionStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        OrganizationOptions organizationOptions = new()
        {
            FacultyName = "факультет тестовий",
            DepartmentName = "кафедра тестова",
            SpecialtyCode = "123",
            SpecialtyName = "Тестова спеціальність",
            StudyForm = "очної форми навчання",
        };

        await DefaultOrganizationSeeder.EnsureAsync(
            _dbContext,
            _userManager,
            _roleManager,
            organizationOptions,
            new BootstrapOptions(),
            NullLogger.Instance);

        Assert.Single(await _dbContext.Faculties.ToListAsync());
        Assert.Single(await _dbContext.Departments.ToListAsync());
        Assert.Single(await _dbContext.Specialties.ToListAsync());
        Assert.Equal(
            (await _dbContext.Departments.SingleAsync()).Id,
            (await _dbContext.DefenceSessions.SingleAsync()).DepartmentId);
    }

    [Fact]
    public async Task EnsureAsync_WhenFacultyAlreadyExists_SkipsSeeding()
    {
        _dbContext.Faculties.Add(new Faculty
        {
            Id = Guid.NewGuid(),
            Name = "Existing",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        await DefaultOrganizationSeeder.EnsureAsync(
            _dbContext,
            _userManager,
            _roleManager,
            new OrganizationOptions { FacultyName = "New", DepartmentName = "New" },
            new BootstrapOptions(),
            NullLogger.Instance);

        Assert.Single(await _dbContext.Faculties.ToListAsync());
        Assert.Empty(await _dbContext.Departments.ToListAsync());
    }

    [Fact]
    public async Task EnsureAsync_AssignsExistingAdminsToDefaultDepartment()
    {
        await EnsureRoleAsync(RoleNames.Admin);
        ApplicationUser admin = await CreateUserAsync("admin@test.local");
        await _userManager.AddToRoleAsync(admin, RoleNames.Admin);

        await DefaultOrganizationSeeder.EnsureAsync(
            _dbContext,
            _userManager,
            _roleManager,
            new OrganizationOptions
            {
                FacultyName = "Факультет",
                DepartmentName = "Кафедра",
                SpecialtyCode = "123",
                SpecialtyName = "Спеціальність",
            },
            new BootstrapOptions(),
            NullLogger.Instance);

        Guid departmentId = await _dbContext.Departments.Select(department => department.Id).SingleAsync();
        bool assigned = await _dbContext.DepartmentAdminAssignments
            .AnyAsync(assignment => assignment.UserId == admin.Id && assignment.DepartmentId == departmentId);

        Assert.True(assigned);
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

    private async Task<ApplicationUser> CreateUserAsync(string email)
    {
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = "Admin User",
            UserKind = UserKind.Employee,
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
