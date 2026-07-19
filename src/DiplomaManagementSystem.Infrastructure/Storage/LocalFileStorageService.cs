using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Application.Storage.Contracts;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Infrastructure.Storage;

internal sealed class LocalFileStorageService(
    IOptions<FileStorageOptions> options,
    IHostEnvironment hostEnvironment) : IFileStorageService
{
    private string RootDirectory => Path.GetFullPath(
        Path.Combine(
            hostEnvironment.ContentRootPath,
            options.Value.Local.RootPath));

    public Task<string> EnsureFolderAsync(
        string? parentFolderId,
        string folderName,
        CancellationToken cancellationToken = default)
    {
        string parentPath = string.IsNullOrWhiteSpace(parentFolderId)
            ? RootDirectory
            : DecodePath(parentFolderId);

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

        string targetPath = GetUniqueTargetPath(parentPath, fileName);
        await using FileStream stream = new(targetPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
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

        return Task.FromResult($"/local-files/{Uri.EscapeDataString(LocalFilePathCodec.EncodePath(path))}");
    }

    internal static string EncodePath(string fullPath) => LocalFilePathCodec.EncodePath(fullPath);

    internal static string DecodePath(string encodedPath) => LocalFilePathCodec.DecodePath(encodedPath);

    private static string GetUniqueTargetPath(string parentPath, string fileName)
    {
        string targetPath = Path.Combine(parentPath, fileName);
        if (!File.Exists(targetPath))
        {
            return targetPath;
        }

        string name = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);
        int counter = 2;
        while (true)
        {
            string candidate = Path.Combine(parentPath, $"{name}_{counter}{extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }

            counter++;
        }
    }
}
