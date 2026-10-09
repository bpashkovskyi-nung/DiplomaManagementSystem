using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Support;

internal static class IntegrationSuperAdminHelper
{
    public static async Task<Guid> CreateSuperAdminUserAsync(
        IServiceProvider serviceProvider,
        bool alsoDepartmentAdmin = false)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager<IdentityRole<Guid>> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await IdentitySeedHelper.EnsureRolesAsync(scope.ServiceProvider);

        string suffix = Guid.NewGuid().ToString("N")[..8];
        string email = $"superadmin.{suffix}@test.local";

        ApplicationUser user = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = "Integration SuperAdmin",
            UserKind = UserKind.Employee,
            CreatedAt = DateTimeOffset.UtcNow,
            EmailConfirmed = true,
        };

        IdentityResult createResult = await userManager.CreateAsync(user);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create super admin user: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
        }

        if (!await roleManager.RoleExistsAsync(RoleNames.SuperAdmin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = RoleNames.SuperAdmin,
                NormalizedName = RoleNames.SuperAdmin.ToUpperInvariant(),
            });
        }

        await userManager.AddToRoleAsync(user, RoleNames.SuperAdmin);

        if (alsoDepartmentAdmin)
        {
            if (!await roleManager.RoleExistsAsync(RoleNames.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = RoleNames.Admin,
                    NormalizedName = RoleNames.Admin.ToUpperInvariant(),
                });
            }

            await userManager.AddToRoleAsync(user, RoleNames.Admin);
            await IntegrationDepartmentHelper.AssignAdminAsync(scope.ServiceProvider, user.Id);
        }

        return user.Id;
    }
}
