using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins;
using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Dtos;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Application.Tests.SuperAdmin;

public sealed class DepartmentAdminAssignmentServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly DepartmentAdminAssignmentService _service;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public DepartmentAdminAssignmentServiceTests()
    {
        string databaseName = Guid.NewGuid().ToString();
        ServiceCollection services = new();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddScoped<DepartmentAdminAssignmentService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _service = _serviceProvider.GetRequiredService<DepartmentAdminAssignmentService>();
        _userManager = _serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    }

    [Fact]
    public async Task AssignAsync_WhenEmployeeOnDepartment_AssignsAdminRole()
    {
        await EnsureAdminRoleAsync();
        (Guid departmentId, Guid userId) = await SeedDepartmentWithEmployeeAsync();

        await _service.AssignAsync(new DepartmentAdminAssignDto(departmentId, userId));

        IReadOnlyList<DepartmentAdminListItemDto> admins =
            await _service.GetByDepartmentAsync(departmentId);
        Assert.Contains(admins, admin => admin.UserId == userId);

        ApplicationUser? user = await _userManager.FindByIdAsync(userId.ToString());
        Assert.NotNull(user);
        Assert.True(await _userManager.IsInRoleAsync(user, RoleNames.Admin));
    }

    [Fact]
    public async Task AssignAsync_WhenAlreadyAssigned_Throws()
    {
        await EnsureAdminRoleAsync();
        (Guid departmentId, Guid userId) = await SeedDepartmentWithEmployeeAsync();
        await _service.AssignAsync(new DepartmentAdminAssignDto(departmentId, userId));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            _service.AssignAsync(new DepartmentAdminAssignDto(departmentId, userId)));

        Assert.Contains("уже призначений", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AssignAsync_WhenUserNotDepartmentEmployee_Throws()
    {
        await EnsureAdminRoleAsync();
        Guid departmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext);
        ApplicationUser outsider = await CreateEmployeeAsync("Outsider", "outsider@test.local");

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            _service.AssignAsync(new DepartmentAdminAssignDto(departmentId, outsider.Id)));

        Assert.Contains("не знайдено на цій кафедрі", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAssignableEmployeesAsync_ExcludesAlreadyAssigned()
    {
        await EnsureAdminRoleAsync();
        (Guid departmentId, Guid userId) = await SeedDepartmentWithEmployeeAsync();
        ApplicationUser second = await CreateEmployeeAsync("Second", "second@test.local");
        _dbContext.DepartmentEmployees.Add(new DepartmentEmployee
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            UserId = second.Id,
            FullName = second.FullName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        await _service.AssignAsync(new DepartmentAdminAssignDto(departmentId, userId));

        IReadOnlyList<DepartmentEmployeeOptionDto> options =
            await _service.GetAssignableEmployeesAsync(departmentId);

        Assert.DoesNotContain(options, option => option.Id == userId);
        Assert.Contains(options, option => option.Id == second.Id);
    }

    [Fact]
    public async Task RemoveAsync_RemovesAssignment()
    {
        await EnsureAdminRoleAsync();
        (Guid departmentId, Guid userId) = await SeedDepartmentWithEmployeeAsync();
        await _service.AssignAsync(new DepartmentAdminAssignDto(departmentId, userId));

        DepartmentAdminListItemDto assignment =
            (await _service.GetByDepartmentAsync(departmentId)).Single(admin => admin.UserId == userId);

        await _service.RemoveAsync(assignment.AssignmentId);

        IReadOnlyList<DepartmentAdminListItemDto> remaining = await _service.GetByDepartmentAsync(departmentId);
        Assert.DoesNotContain(remaining, admin => admin.UserId == userId);
    }

    private async Task<(Guid DepartmentId, Guid UserId)> SeedDepartmentWithEmployeeAsync()
    {
        Guid departmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext);
        ApplicationUser employee = await CreateEmployeeAsync("Dept Employee", "dept.employee@test.local");
        _dbContext.DepartmentEmployees.Add(new DepartmentEmployee
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            UserId = employee.Id,
            FullName = employee.FullName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return (departmentId, employee.Id);
    }

    private async Task EnsureAdminRoleAsync()
    {
        if (!await _roleManager.RoleExistsAsync(RoleNames.Admin))
        {
            await _roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = RoleNames.Admin,
                NormalizedName = RoleNames.Admin.ToUpperInvariant(),
            });
        }
    }

    private async Task<ApplicationUser> CreateEmployeeAsync(string fullName, string email)
    {
        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = fullName,
            UserKind = UserKind.Employee,
            CreatedAt = DateTimeOffset.UtcNow,
            EmailConfirmed = true,
        };

        IdentityResult result = await _userManager.CreateAsync(user);
        Assert.True(result.Succeeded);
        return user;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}
