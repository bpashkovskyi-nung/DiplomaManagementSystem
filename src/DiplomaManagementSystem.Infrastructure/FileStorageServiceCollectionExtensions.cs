using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Storage.Contracts;
using DiplomaManagementSystem.Infrastructure.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Infrastructure;

internal static class FileStorageServiceCollectionExtensions
{
    public static IServiceCollection AddFileStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName));

        FileStorageOptions options = configuration
            .GetSection(FileStorageOptions.SectionName)
            .Get<FileStorageOptions>() ?? new FileStorageOptions();

        if (string.Equals(options.Provider, "Google", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IFileStorageService, GoogleDriveFileStorageService>();
        }
        else
        {
            services.AddSingleton<IFileStorageService, LocalFileStorageService>();
        }

        return services;
    }
}
