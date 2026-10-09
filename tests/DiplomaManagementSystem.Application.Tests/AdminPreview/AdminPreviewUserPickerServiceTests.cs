using DiplomaManagementSystem.Application.AdminPreview;
using DiplomaManagementSystem.Application.AdminPreview.Contracts;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Application.Tests.AdminPreview;

public sealed class AdminPreviewUserPickerServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly TestDepartmentContext _departmentContext;
    private readonly AdminPreviewUserPickerService _service;

    public AdminPreviewUserPickerServiceTests()
    {
        string databaseName = Guid.NewGuid().ToString();
        ServiceCollection services = new();
        services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
        services.AddSingleton<TestDepartmentContext>();
        services.AddSingleton<IDepartmentContext>(provider => provider.GetRequiredService<TestDepartmentContext>());
        services.AddScoped<IDepartmentAuthorizationService, DepartmentAuthorizationService>();
        services.AddScoped<CurrentDepartmentResolver>();
        services.AddScoped<IUserDisplayQueries, DiplomaManagementSystem.Infrastructure.Persistence.Queries.UserDisplayQueries>();
        services.AddScoped<AdminPreviewUserPickerService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _departmentContext = _serviceProvider.GetRequiredService<TestDepartmentContext>();
        _service = _serviceProvider.GetRequiredService<AdminPreviewUserPickerService>();
    }

    [Fact]
    public async Task GetUsersAsync_WhenDepartmentScoped_ReturnsOnlyDepartmentEmployees()
    {
        (Guid departmentA, Guid departmentB, Guid employeeAId, Guid employeeBId) = await SeedTwoDepartmentsAsync();
        _departmentContext.CurrentDepartmentId = departmentA;

        IReadOnlyList<AdminPreviewUserOption> users =
            await _service.GetUsersAsync(UserKind.Employee, CancellationToken.None);

        Assert.Contains(users, user => user.Id == employeeAId);
        Assert.DoesNotContain(users, user => user.Id == employeeBId);
        Assert.NotEqual(departmentB, departmentA);
    }

    [Fact]
    public async Task GetUsersAsync_WhenDepartmentScoped_FiltersRoleLabelsByDepartmentSessions()
    {
        (Guid departmentA, Guid _, Guid employeeAId, Guid employeeBId) = await SeedTwoDepartmentsAsync();
        _departmentContext.CurrentDepartmentId = departmentA;

        IReadOnlyList<AdminPreviewUserOption> users =
            await _service.GetUsersAsync(UserKind.Employee, CancellationToken.None);

        AdminPreviewUserOption? employeeA = users.SingleOrDefault(user => user.Id == employeeAId);
        AdminPreviewUserOption? employeeB = users.SingleOrDefault(user => user.Id == employeeBId);

        Assert.NotNull(employeeA);
        Assert.Null(employeeB);
        Assert.Contains("Секретар", employeeA.Subtitle, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetUsersAsync_WhenNoDepartmentContext_ReturnsEmpty()
    {
        await SeedTwoDepartmentsAsync();
        _departmentContext.CurrentDepartmentId = null;

        IReadOnlyList<AdminPreviewUserOption> users =
            await _service.GetUsersAsync(UserKind.Employee, CancellationToken.None);

        Assert.Empty(users);
    }

    private async Task<(Guid DepartmentA, Guid DepartmentB, Guid EmployeeAId, Guid EmployeeBId)> SeedTwoDepartmentsAsync()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string suffix = Guid.NewGuid().ToString("N")[..6];

        Faculty faculty = new()
        {
            Id = Guid.NewGuid(),
            Name = "Факультет",
            IsActive = true,
            CreatedAt = now,
        };
        Department departmentA = new()
        {
            Id = Guid.NewGuid(),
            FacultyId = faculty.Id,
            Name = "Кафедра A",
            IsActive = true,
            CreatedAt = now,
        };
        Department departmentB = new()
        {
            Id = Guid.NewGuid(),
            FacultyId = faculty.Id,
            Name = "Кафедра B",
            IsActive = true,
            CreatedAt = now,
        };
        ApplicationUser employeeA = CreateEmployee($"Employee A {suffix}", $"a.{suffix}@test.local");
        ApplicationUser employeeB = CreateEmployee($"Employee B {suffix}", $"b.{suffix}@test.local");
        DefenceSession sessionA = new()
        {
            Id = Guid.NewGuid(),
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Semester = 1,
            Status = DefenceSessionStatus.Active,
            DepartmentId = departmentA.Id,
            CreatedAt = now,
        };
        DefenceSession sessionB = new()
        {
            Id = Guid.NewGuid(),
            Year = 2027,
            Type = DefenceSessionType.Bachelor,
            Semester = 1,
            Status = DefenceSessionStatus.Active,
            DepartmentId = departmentB.Id,
            CreatedAt = now,
        };

        _dbContext.Faculties.Add(faculty);
        _dbContext.Departments.AddRange(departmentA, departmentB);
        _dbContext.Users.AddRange(employeeA, employeeB);
        _dbContext.DepartmentEmployees.AddRange(
            new DepartmentEmployee
            {
                Id = Guid.NewGuid(),
                DepartmentId = departmentA.Id,
                UserId = employeeA.Id,
                FullName = employeeA.FullName,
                IsActive = true,
                CreatedAt = now,
            },
            new DepartmentEmployee
            {
                Id = Guid.NewGuid(),
                DepartmentId = departmentB.Id,
                UserId = employeeB.Id,
                FullName = employeeB.FullName,
                IsActive = true,
                CreatedAt = now,
            });
        _dbContext.DefenceSessions.AddRange(sessionA, sessionB);
        _dbContext.AnnualRoleAssignments.AddRange(
            new AnnualRoleAssignment
            {
                Id = Guid.NewGuid(),
                DefenceSessionId = sessionA.Id,
                EmployeeId = employeeA.Id,
                RoleType = AnnualRoleType.ExamCommissionSecretary,
                AssignedAt = now,
            },
            new AnnualRoleAssignment
            {
                Id = Guid.NewGuid(),
                DefenceSessionId = sessionB.Id,
                EmployeeId = employeeB.Id,
                RoleType = AnnualRoleType.ExamCommissionSecretary,
                AssignedAt = now,
            });
        await _dbContext.SaveChangesAsync();

        return (departmentA.Id, departmentB.Id, employeeA.Id, employeeB.Id);
    }

    private static ApplicationUser CreateEmployee(string fullName, string email) =>
        new()
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

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }
}
