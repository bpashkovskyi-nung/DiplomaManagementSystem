using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Application.Storage.Contracts;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DiplomaManagementSystem.Integration.Tests.Web;

public sealed class DiplomaManagementSystemWebApplicationFactory(string connectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", connectionString);
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["Bootstrap:AdminEmail"] = string.Empty,
                ["Authentication:Google:ClientId"] = "integration-test-client-id",
                ["Authentication:Google:ClientSecret"] = "integration-test-client-secret",
                ["FileStorage:Provider"] = "Local",
                ["FileStorage:Local:RootPath"] = "App_Data/test-diploma-files",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.PostConfigure<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>(options =>
            {
                options.ClientId = "integration-test-client-id";
                options.ClientSecret = "integration-test-client-secret";
            });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFileStorageService>();
            services.AddSingleton<IFileStorageService, TestLocalFileStorageService>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = IntegrationTestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = IntegrationTestAuthHandler.SchemeName;
                    options.DefaultForbidScheme = IntegrationTestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, IntegrationTestAuthHandler>(
                    IntegrationTestAuthHandler.SchemeName,
                    _ => { });
        });
    }
}

internal sealed class TestLocalFileStorageService(IWebHostEnvironment hostEnvironment) : IFileStorageService
{
    private string RootDirectory => Path.GetFullPath(Path.Combine(hostEnvironment.ContentRootPath, "App_Data/test-diploma-files"));

    public Task<string> EnsureFolderAsync(string? parentFolderId, string folderName, CancellationToken cancellationToken = default)
    {
        string parentPath = string.IsNullOrWhiteSpace(parentFolderId) ? RootDirectory : DecodePath(parentFolderId);
        string folderPath = Path.Combine(parentPath, folderName);
        Directory.CreateDirectory(folderPath);
        return Task.FromResult(EncodePath(folderPath));
    }

    public async Task<StoredFileResult> UploadFileAsync(
        string parentFolderId,
        string fileName,
        UploadFileContent content,
        CancellationToken cancellationToken = default)
    {
        string parentPath = DecodePath(parentFolderId);
        Directory.CreateDirectory(parentPath);

        string targetPath = Path.Combine(parentPath, fileName);
        await using FileStream stream = new(targetPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await content.Content.CopyToAsync(stream, cancellationToken);

        return new StoredFileResult(
            EncodePath(targetPath),
            Path.GetFileName(targetPath),
            content.ContentType,
            content.Length);
    }

    public Task<string> GetViewUrlAsync(string fileId, CancellationToken cancellationToken = default)
    {
        string path = DecodePath(fileId);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Stored file was not found.", path);
        }

        return Task.FromResult($"/local-files/{Uri.EscapeDataString(fileId)}");
    }

    private static string EncodePath(string path) =>
        Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(path));

    private static string DecodePath(string encodedPath) =>
        System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedPath));
}
