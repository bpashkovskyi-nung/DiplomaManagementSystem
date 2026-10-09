using DiplomaManagementSystem.Application.Admin.Employees;
using DiplomaManagementSystem.Application.Admin.Employees.Dtos;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Security;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Application.Tests.Admin;

public sealed class EmployeeAdminServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly EmployeeAdminService _service;
    private readonly Guid _departmentId;

    public EmployeeAdminServiceTests()
    {
        string databaseName = Guid.NewGuid().ToString();
        ServiceCollection services = new();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddLogging();
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(identityOptions => { identityOptions.User.RequireUniqueEmail = true; })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        AdminDepartmentTestSupport.RegisterDepartmentServices(services);
        services.AddSingleton(new EmailDomainValidator(Microsoft.Extensions.Options.Options.Create(new SecurityOptions())));
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<EmployeeAdminService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _departmentId = AdminDepartmentTestSupport.SeedDefaultDepartmentAsync(_dbContext).GetAwaiter().GetResult();
        _service = _serviceProvider.GetRequiredService<EmployeeAdminService>();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmployees()
    {
        await SeedEmployeeAsync("Teacher One", "teacher@test.com");

        IReadOnlyList<EmployeeListItemDto> items = await _service.GetAllAsync();

        EmployeeListItemDto item = Assert.Single(items);
        Assert.Equal("Teacher One", item.FullName);
    }

    [Fact]
    public async Task CreateAsync_WhenValid_CreatesEmployee()
    {
        await SeedRoleAsync(RoleNames.Employee);

        Guid employeeId = await _service.CreateAsync(new EmployeeFormDto(null, "Teacher One", "teacher@test.com"));

        ApplicationUser? employee = await _dbContext.Users.FindAsync(employeeId);
        Assert.NotNull(employee);
        Assert.Equal(UserKind.Employee, employee.UserKind);
    }

    [Fact]
    public async Task CreateAsync_WhenDuplicateEmail_Throws()
    {
        await SeedRoleAsync(RoleNames.Employee);
        await _service.CreateAsync(new EmployeeFormDto(null, "Teacher One", "teacher@test.com"));

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new EmployeeFormDto(null, "Teacher Two", "teacher@test.com")));
    }

    [Fact]
    public async Task DeleteAsync_WhenSupervisorOnDiploma_Throws()
    {
        Guid employeeId = await SeedEmployeeAsync("Teacher One", "teacher@test.com");
        _dbContext.Diplomas.Add(new Diploma
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            SupervisorId = employeeId,
            DefenceSessionId = Guid.NewGuid(),
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Confirmed,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
            LifecycleStatus = DiplomaLifecycleStatus.SupervisorConfirmed,
            AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => _service.DeleteAsync(employeeId));
    }

    [Fact]
    public async Task DeleteAsync_WhenNoAssignments_RemovesEmployee()
    {
        await SeedRoleAsync(RoleNames.Employee);
        Guid employeeId = await _service.CreateAsync(new EmployeeFormDto(null, "Teacher One", "teacher@test.com"));

        await _service.DeleteAsync(employeeId);

        Assert.Null(await _dbContext.Users.FindAsync(employeeId));
    }

    private async Task<Guid> SeedEmployeeAsync(string fullName, string email)
    {
        var employeeId = Guid.NewGuid();
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = employeeId,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = fullName,
            UserKind = UserKind.Employee,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        _dbContext.DepartmentEmployees.Add(new DepartmentEmployee
        {
            Id = Guid.NewGuid(),
            DepartmentId = _departmentId,
            UserId = employeeId,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return employeeId;
    }

    private async Task SeedRoleAsync(string roleName)
    {
        RoleManager<IdentityRole<Guid>> roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
            });
        }
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _dbContext.Dispose();
    }
}
