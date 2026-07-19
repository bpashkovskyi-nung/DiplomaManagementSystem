using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Application.Storage.Contracts;

using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Upload;

using Microsoft.Extensions.Options;

using DriveFile = Google.Apis.Drive.v3.Data.File;

namespace DiplomaManagementSystem.Infrastructure.Storage;

internal sealed class GoogleDriveFileStorageService(IOptions<FileStorageOptions> options) : IFileStorageService
{
    private const string FolderMimeType = "application/vnd.google-apps.folder";

    public async Task<string> EnsureFolderAsync(
        string? parentFolderId,
        string folderName,
        CancellationToken cancellationToken = default)
    {
        GoogleDriveOptions driveOptions = options.Value.GoogleDrive;
        DriveService service = CreateDriveService();

        string parent = parentFolderId ?? driveOptions.RootFolderId
            ?? throw new InvalidOperationException("Google Drive root folder id is not configured.");

        string? existingFolderId = await FindChildFolderIdAsync(service, parent, folderName, cancellationToken);
        if (existingFolderId is not null)
        {
            return existingFolderId;
        }

        DriveFile folderMetadata = new()
        {
            Name = folderName,
            MimeType = FolderMimeType,
            Parents = [parent],
        };

        FilesResource.CreateRequest request = service.Files.Create(folderMetadata);
        request.SupportsAllDrives = true;
        request.Fields = "id";
        DriveFile created = await request.ExecuteAsync(cancellationToken);
        return created.Id;
    }

    public async Task<StoredFileResult> UploadFileAsync(
        string parentFolderId,
        string fileName,
        UploadFileContent content,
        CancellationToken cancellationToken = default)
    {
        DriveService service = CreateDriveService();

        DriveFile fileMetadata = new()
        {
            Name = fileName,
            Parents = [parentFolderId],
        };

        FilesResource.CreateMediaUpload uploadRequest = service.Files.Create(fileMetadata, content.Content, content.ContentType);
        uploadRequest.SupportsAllDrives = true;
        uploadRequest.Fields = "id,name,mimeType,size";
        IUploadProgress progress = await uploadRequest.UploadAsync(cancellationToken);
        if (progress.Status != UploadStatus.Completed || uploadRequest.ResponseBody is null)
        {
            throw new InvalidOperationException(progress.Exception?.Message ?? "Google Drive upload failed.");
        }

        DriveFile uploaded = uploadRequest.ResponseBody;
        return new StoredFileResult(
            uploaded.Id,
            uploaded.Name,
            uploaded.MimeType ?? content.ContentType,
            uploaded.Size ?? content.Length);
    }

    public async Task<string> GetViewUrlAsync(string fileId, CancellationToken cancellationToken = default)
    {
        DriveService service = CreateDriveService();
        FilesResource.GetRequest request = service.Files.Get(fileId);
        request.SupportsAllDrives = true;
        request.Fields = "webViewLink";
        DriveFile file = await request.ExecuteAsync(cancellationToken);
        return file.WebViewLink ?? $"https://drive.google.com/file/d/{fileId}/view";
    }

    private DriveService CreateDriveService()
    {
        GoogleDriveOptions driveOptions = options.Value.GoogleDrive;
        if (string.IsNullOrWhiteSpace(driveOptions.ServiceAccountJsonPath))
        {
            throw new InvalidOperationException("Google Drive service account json path is not configured.");
        }

        if (!System.IO.File.Exists(driveOptions.ServiceAccountJsonPath))
        {
            throw new FileNotFoundException("Google Drive service account json file was not found.", driveOptions.ServiceAccountJsonPath);
        }

        GoogleCredential credential;
        using (FileStream stream = new(driveOptions.ServiceAccountJsonPath, FileMode.Open, FileAccess.Read))
        {
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(DriveService.Scope.Drive);
        }

        if (!string.IsNullOrWhiteSpace(driveOptions.ImpersonateUserEmail))
        {
            credential = credential.CreateWithUser(driveOptions.ImpersonateUserEmail);
        }

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Diploma Management System",
        });
    }

    private static async Task<string?> FindChildFolderIdAsync(
        DriveService service,
        string parentFolderId,
        string folderName,
        CancellationToken cancellationToken)
    {
        string escapedName = folderName.Replace("'", "\\'", StringComparison.Ordinal);
        FilesResource.ListRequest request = service.Files.List();
        request.SupportsAllDrives = true;
        request.IncludeItemsFromAllDrives = true;
        request.Corpora = "allDrives";
        request.Q = $"mimeType='{FolderMimeType}' and name='{escapedName}' and '{parentFolderId}' in parents and trashed=false";
        request.Fields = "files(id,name)";
        FileList response = await request.ExecuteAsync(cancellationToken);
        return response.Files?.FirstOrDefault()?.Id;
    }
}
