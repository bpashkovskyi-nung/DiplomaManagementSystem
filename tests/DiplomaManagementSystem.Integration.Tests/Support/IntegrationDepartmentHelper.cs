using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Support;

internal sealed record SecondDepartmentSeed(Guid DepartmentId, Guid AdminId, Guid SessionId, int Year);

internal static class IntegrationDepartmentHelper
{
    public static async Task<SecondDepartmentSeed> CreateSecondDepartmentWithAdminAndSessionAsync(
        IServiceProvider serviceProvider,
        int year)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        IApplicationDbContext dbContext = services.GetRequiredService<IApplicationDbContext>();
        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager<IdentityRole<Guid>> roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        DateTimeOffset now = DateTimeOffset.UtcNow;
        string suffix = Guid.NewGuid().ToString("N")[..8];

        Faculty faculty = new()
        {
            Id = Guid.NewGuid(),
            Name = $"Факультет {suffix}",
            IsActive = true,
            CreatedAt = now,
        };
        Department department = new()
        {
            Id = Guid.NewGuid(),
            FacultyId = faculty.Id,
            Name = $"Кафедра {suffix}",
            IsActive = true,
            CreatedAt = now,
        };
        Specialty specialty = new()
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            Code = "999",
            Name = $"Спеціальність {suffix}",
            IsActive = true,
            CreatedAt = now,
        };
        DefenceSession session = new()
        {
            Id = Guid.NewGuid(),
            Year = year,
            Type = DefenceSessionType.Bachelor,
            Semester = 1,
            Status = DefenceSessionStatus.Active,
            DepartmentId = department.Id,
            CreatedAt = now,
        };

        dbContext.Faculties.Add(faculty);
        dbContext.Departments.Add(department);
        dbContext.Specialties.Add(specialty);
        dbContext.DefenceSessions.Add(session);
        await dbContext.SaveChangesAsync();

        if (!await roleManager.RoleExistsAsync(RoleNames.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = RoleNames.Admin,
                NormalizedName = RoleNames.Admin.ToUpperInvariant(),
            });
        }

        string email = $"admin.{suffix}@test.local";
        ApplicationUser admin = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = $"Admin {suffix}",
            UserKind = UserKind.Employee,
            CreatedAt = now,
            EmailConfirmed = true,
        };

        IdentityResult createResult = await userManager.CreateAsync(admin);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create second-department admin: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
        }

        await userManager.AddToRoleAsync(admin, RoleNames.Admin);

        dbContext.DepartmentAdminAssignments.Add(new DepartmentAdminAssignment
        {
            Id = Guid.NewGuid(),
            DepartmentId = department.Id,
            UserId = admin.Id,
            AssignedAt = now,
        });
        await dbContext.SaveChangesAsync();

        return new SecondDepartmentSeed(department.Id, admin.Id, session.Id, year);
    }


    public static async Task<Guid> GetDefaultSpecialtyIdAsync(IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        Guid departmentId = await GetDefaultDepartmentIdAsync(scope.ServiceProvider);

        return await dbContext.Specialties
            .AsNoTracking()
            .Where(specialty => specialty.DepartmentId == departmentId && specialty.IsActive)
            .OrderBy(specialty => specialty.CreatedAt)
            .ThenBy(specialty => specialty.Id)
            .Select(specialty => specialty.Id)
            .FirstAsync();
    }

    public static async Task<Guid> GetDefaultDepartmentIdAsync(IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        Guid departmentId = await dbContext.Departments
            .AsNoTracking()
            .Where(department => department.IsActive)
            .OrderBy(department => department.CreatedAt)
            .ThenBy(department => department.Id)
            .Select(department => department.Id)
            .FirstAsync();

        return departmentId;
    }

    public static async Task AssignAdminAsync(IServiceProvider serviceProvider, Guid userId)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        await EnsureDefaultDepartmentContextAsync(scope.ServiceProvider);
        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        Guid departmentId = await GetDefaultDepartmentIdAsync(scope.ServiceProvider);

        bool alreadyAssigned = await dbContext.DepartmentAdminAssignments
            .AnyAsync(assignment => assignment.UserId == userId && assignment.DepartmentId == departmentId);

        if (alreadyAssigned)
        {
            return;
        }

        dbContext.DepartmentAdminAssignments.Add(new DepartmentAdminAssignment
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            UserId = userId,
            AssignedAt = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync();
    }

    public static async Task AssignEmployeeAsync(
        IServiceProvider serviceProvider,
        Guid userId,
        string fullName)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        IApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        Guid departmentId = await GetDefaultDepartmentIdAsync(scope.ServiceProvider);

        bool alreadyAssigned = await dbContext.DepartmentEmployees
            .AnyAsync(employee => employee.UserId == userId && employee.DepartmentId == departmentId);

        if (alreadyAssigned)
        {
            return;
        }

        dbContext.DepartmentEmployees.Add(new DepartmentEmployee
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            UserId = userId,
            FullName = fullName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await dbContext.SaveChangesAsync();
    }

    public static async Task EnsureDefaultDepartmentContextAsync(IServiceProvider serviceProvider)
    {
        IntegrationTestDepartmentContext departmentContext =
            serviceProvider.GetRequiredService<IntegrationTestDepartmentContext>();
        departmentContext.CurrentDepartmentId = await GetDefaultDepartmentIdAsync(serviceProvider);
    }
}
