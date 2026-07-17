using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Infrastructure;
using DiplomaManagementSystem.Infrastructure.Persistence;
using DiplomaManagementSystem.Infrastructure.Persistence.Seeding;
using DiplomaManagementSystem.Integration.Tests.Support;
using DiplomaManagementSystem.Integration.Tests.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace DiplomaManagementSystem.Integration.Tests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private const string ExternalConnectionEnvironmentVariable = "DIPLOMA_INTEGRATION_PG";

    private PostgreSqlContainer? _container;

    private string? _connectionString;

    private readonly IntegrationTestDepartmentContext _departmentContext = new();

    public bool IsAvailable { get; private set; }

    public string SkipReason { get; private set; } = "PostgreSQL test database is not available.";

    public string ConnectionString =>
        _connectionString
        ?? throw new InvalidOperationException("PostgreSQL test database is not initialized.");

    internal IntegrationTestDepartmentContext DepartmentContext => _departmentContext;

    public async Task InitializeAsync()
    {
        string? externalConnection = Environment.GetEnvironmentVariable(ExternalConnectionEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(externalConnection))
        {
            _connectionString = externalConnection;
            await InitializeDatabaseAsync();
            IsAvailable = true;
            return;
        }

        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("diploma_test")
                .WithUsername("test")
                .WithPassword("test")
                .Build();

            await _container.StartAsync();
            _connectionString = _container.GetConnectionString();
            await InitializeDatabaseAsync();
            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            SkipReason = $"PostgreSQL is required for integration tests: {ex.Message}";
        }
    }

    private async Task InitializeDatabaseAsync()
    {
        await using AsyncServiceScope scope = CreateProvider().CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
        await IdentitySeedHelper.EnsureRolesAsync(scope.ServiceProvider);
        await OrganizationSeedHelper.EnsureDefaultOrganizationAsync(scope.ServiceProvider);
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(scope.ServiceProvider);
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public DiplomaManagementSystemWebApplicationFactory CreateWebFactory() =>
        new(ConnectionString);

    public ServiceProvider CreateProvider()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Bootstrap:AdminEmail"] = string.Empty,
                ["FileStorage:Provider"] = "Local",
                ["FileStorage:Local:RootPath"] = "files",
                ["Organization:FacultyName"] = "факультет інформаційних технологій",
                ["Organization:DepartmentName"] = "кафедра комп'ютерних систем і мереж",
                ["Organization:SpecialtyCode"] = "123",
                ["Organization:SpecialtyName"] = "Комп'ютерна інженерія",
                ["Organization:StudyForm"] = "очної форми навчання",
            })
            .Build();

        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment, IntegrationTestHostEnvironment>();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddSingleton(_departmentContext);
        services.AddScoped<DiplomaManagementSystem.Application.Departments.Contracts.IDepartmentContext>(
            _ => _departmentContext);

        foreach (ServiceDescriptor descriptor in services
                     .Where(service => service.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                     .ToList())
        {
            services.Remove(descriptor);
        }

        return services.BuildServiceProvider();
    }
}

internal sealed class IntegrationTestDepartmentContext : DiplomaManagementSystem.Application.Departments.Contracts.IDepartmentContext
{
    public Guid? CurrentDepartmentId { get; set; }

    public bool IsSuperAdminImpersonating { get; set; }
}

[CollectionDefinition(nameof(IntegrationCollection))]
public sealed class IntegrationCollection : ICollectionFixture<PostgreSqlFixture>;

internal static class IdentitySeedHelper
{
    public static async Task EnsureRolesAsync(IServiceProvider serviceProvider)
    {
        RoleManager<IdentityRole<Guid>> roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
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

internal static class OrganizationSeedHelper
{
    public static async Task EnsureDefaultOrganizationAsync(IServiceProvider serviceProvider)
    {
        ApplicationDbContext dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        if (await dbContext.Faculties.AnyAsync())
        {
            return;
        }

        IOptions<OrganizationOptions> organizationOptions =
            serviceProvider.GetRequiredService<IOptions<OrganizationOptions>>();
        IOptions<BootstrapOptions> bootstrapOptions =
            serviceProvider.GetRequiredService<IOptions<BootstrapOptions>>();
        ILogger<DefaultOrganizationMigrator> logger =
            serviceProvider.GetRequiredService<ILogger<DefaultOrganizationMigrator>>();

        await DefaultOrganizationSeeder.EnsureAsync(
            dbContext,
            serviceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
            serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>(),
            organizationOptions.Value,
            bootstrapOptions.Value,
            logger);
    }
}

internal static class IntegrationTestGuards
{
    public static void RequireDatabase(PostgreSqlFixture fixture)
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);
    }
}
