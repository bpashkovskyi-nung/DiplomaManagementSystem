using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Organization;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Seeding;

internal static class DefaultOrganizationSeeder
{
    public static async Task EnsureAsync(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        OrganizationOptions organizationOptions,
        BootstrapOptions bootstrapOptions,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Faculties.AnyAsync(cancellationToken))
        {
            return;
        }

        DateTimeOffset now = DateTimeOffset.UtcNow;
        (Faculty faculty, Department department, Specialty specialty) =
            DefaultOrganizationSeedBuilder.FromOptions(organizationOptions, now);

        dbContext.Faculties.Add(faculty);
        dbContext.Departments.Add(department);
        dbContext.Specialties.Add(specialty);

        List<DefenceSession> sessions = await dbContext.DefenceSessions.ToListAsync(cancellationToken);
        foreach (DefenceSession session in sessions)
        {
            session.DepartmentId = department.Id;
        }

        List<ApplicationUser> employees = await dbContext.Users
            .Where(user => user.UserKind == UserKind.Employee)
            .ToListAsync(cancellationToken);

        foreach (ApplicationUser employee in employees)
        {
            dbContext.DepartmentEmployees.Add(new DepartmentEmployee
            {
                Id = Guid.NewGuid(),
                DepartmentId = department.Id,
                UserId = employee.Id,
                FullName = employee.FullName,
                AcademicRank = employee.AcademicRank,
                ShortDisplayName = employee.ShortDisplayName,
                IsActive = true,
                CreatedAt = now,
            });
        }

        await EnsureSuperAdminRoleAsync(roleManager, cancellationToken);

        IList<ApplicationUser> admins = await userManager.GetUsersInRoleAsync(RoleNames.Admin);
        foreach (ApplicationUser admin in admins)
        {
            dbContext.DepartmentAdminAssignments.Add(new DepartmentAdminAssignment
            {
                Id = Guid.NewGuid(),
                DepartmentId = department.Id,
                UserId = admin.Id,
                AssignedAt = now,
            });
        }

        string bootstrapEmail = bootstrapOptions.AdminEmail.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(bootstrapEmail))
        {
            ApplicationUser? bootstrapUser = await userManager.FindByEmailAsync(bootstrapEmail);
            if (bootstrapUser is not null && !await userManager.IsInRoleAsync(bootstrapUser, RoleNames.SuperAdmin))
            {
                await userManager.AddToRoleAsync(bootstrapUser, RoleNames.SuperAdmin);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Seeded default organization: faculty '{Faculty}', department '{Department}', sessions={SessionCount}, employees={EmployeeCount}.",
            faculty.Name,
            department.Name,
            sessions.Count,
            employees.Count);
    }

    private static async Task EnsureSuperAdminRoleAsync(
        RoleManager<IdentityRole<Guid>> roleManager,
        CancellationToken cancellationToken)
    {
        if (await roleManager.RoleExistsAsync(RoleNames.SuperAdmin))
        {
            return;
        }

        IdentityResult result = await roleManager.CreateAsync(new IdentityRole<Guid>
        {
            Id = Guid.NewGuid(),
            Name = RoleNames.SuperAdmin,
            NormalizedName = RoleNames.SuperAdmin.ToUpperInvariant(),
        });

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create {RoleNames.SuperAdmin} role: {string.Join("; ", result.Errors.Select(error => error.Description))}");
        }
    }
}
