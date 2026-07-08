using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests.Support;

internal static class IntegrationAdminHelper
{
    public static async Task<Guid> CreateAdminUserAsync(IServiceProvider serviceProvider)
    {
        await using AsyncServiceScope scope = serviceProvider.CreateAsyncScope();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        RoleManager<IdentityRole<Guid>> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await IdentitySeedHelper.EnsureRolesAsync(scope.ServiceProvider);

        string suffix = Guid.NewGuid().ToString("N")[..8];
        string email = $"admin.{suffix}@test.local";

        ApplicationUser admin = new()
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = "Integration Admin",
            UserKind = UserKind.Employee,
            CreatedAt = DateTimeOffset.UtcNow,
            EmailConfirmed = true,
        };

        IdentityResult createResult = await userManager.CreateAsync(admin);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create admin user: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
        }

        if (!await roleManager.RoleExistsAsync(RoleNames.Admin))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = RoleNames.Admin,
                NormalizedName = RoleNames.Admin.ToUpperInvariant(),
            });
        }

        await userManager.AddToRoleAsync(admin, RoleNames.Admin);
        await IntegrationDepartmentHelper.AssignAdminAsync(serviceProvider, admin.Id);
        return admin.Id;
    }
}
