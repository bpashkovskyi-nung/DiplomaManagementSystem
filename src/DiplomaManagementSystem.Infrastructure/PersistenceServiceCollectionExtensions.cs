using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Infrastructure.Persistence;
using DiplomaManagementSystem.Infrastructure.Persistence.Queries;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Infrastructure;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection")
                                  ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddSingleton<PostgresSearchPathInterceptor>();
        services.AddSingleton<DiplomaWorkflowSaveChangesInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            options.UseNpgsql(connectionString)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<PostgresSearchPathInterceptor>(),
                    serviceProvider.GetRequiredService<DiplomaWorkflowSaveChangesInterceptor>()));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddHostedService<BootstrapAdminSeeder>();

        services.AddScoped<IDiplomaQueries, DiplomaQueries>();
        services.AddScoped<IUserDisplayQueries, UserDisplayQueries>();
        services.AddScoped<ITopicVersionQueries, TopicVersionQueries>();
        services.AddScoped<IAnnualRoleQueries, AnnualRoleQueries>();
        services.AddScoped<IDefenceSessionQueries, DefenceSessionQueries>();
        services.AddScoped<IStudyGroupQueries, StudyGroupQueries>();
        services.AddScoped<IDiplomaCommentQueries, DiplomaCommentQueries>();
        services.AddScoped<IDiplomaDocumentQueries, DiplomaDocumentQueries>();
        services.AddScoped<IEmployeeHomeQueries, EmployeeHomeQueries>();
        services.AddScoped<IAdmissionStepQueries, AdmissionStepQueries>();
        services.AddScoped<IEmployeeWorkloadLimitQueries, EmployeeWorkloadLimitQueries>();

        return services;
    }
}
