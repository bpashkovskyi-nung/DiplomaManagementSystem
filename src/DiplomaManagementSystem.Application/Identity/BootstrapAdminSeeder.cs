using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Application.Identity;

internal sealed class BootstrapAdminSeeder(
    IServiceProvider serviceProvider,
    IOptions<BootstrapOptions> bootstrapOptions,
    ILogger<BootstrapAdminSeeder> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        string adminEmail = bootstrapOptions.Value.AdminEmail.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            logger.LogWarning("Bootstrap:AdminEmail is not configured; skipping admin seed.");
            return;
        }

        using IServiceScope scope = serviceProvider.CreateScope();
        RoleManager<IdentityRole<Guid>> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        await EnsureRolesAsync(roleManager);

        ApplicationUser? admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                NormalizedEmail = adminEmail.ToUpperInvariant(),
                NormalizedUserName = adminEmail.ToUpperInvariant(),
                FullName = "System Administrator",
                UserKind = UserKind.Employee,
                CreatedAt = DateTimeOffset.UtcNow,
                EmailConfirmed = true,
            };

            IdentityResult createResult = await userManager.CreateAsync(admin);
            if (!createResult.Succeeded)
            {
                logger.LogError("Failed to create bootstrap admin: {Errors}",
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Bootstrap admin user created for {Email}.", adminEmail);
        }

        if (!await userManager.IsInRoleAsync(admin, RoleNames.Admin))
        {
            await userManager.AddToRoleAsync(admin, RoleNames.Admin);
            logger.LogInformation("Admin role assigned to {Email}.", adminEmail);
        }

        if (!await userManager.IsInRoleAsync(admin, RoleNames.SuperAdmin))
        {
            await userManager.AddToRoleAsync(admin, RoleNames.SuperAdmin);
            logger.LogInformation("SuperAdmin role assigned to {Email}.", adminEmail);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        string[] roles = [RoleNames.SuperAdmin, RoleNames.Admin, RoleNames.Student, RoleNames.Employee];

        foreach (string role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = role,
                    NormalizedName = role.ToUpperInvariant(),
                });
            }
        }
    }
}
