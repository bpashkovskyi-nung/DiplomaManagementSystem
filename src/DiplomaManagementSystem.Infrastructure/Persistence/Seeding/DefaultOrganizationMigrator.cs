using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Infrastructure.Persistence.Seeding;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Infrastructure.Persistence.Seeding;

internal sealed class DefaultOrganizationMigrator(
    IServiceProvider serviceProvider,
    IOptions<OrganizationOptions> organizationOptions,
    IOptions<BootstrapOptions> bootstrapOptions,
    ILogger<DefaultOrganizationMigrator> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await DefaultOrganizationSeeder.EnsureAsync(
            dbContext,
            scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            organizationOptions.Value,
            bootstrapOptions.Value,
            logger,
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
