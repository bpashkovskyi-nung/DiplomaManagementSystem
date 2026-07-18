using DiplomaManagementSystem.Application.Common.Contracts;
using DiplomaManagementSystem.Application.Documents.Contracts;
using DiplomaManagementSystem.Application.Documents.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Persistence;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Application.Storage.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;
using Microsoft.Extensions.Options;

namespace DiplomaManagementSystem.Application.Documents;

internal sealed class DiplomaDocumentService(
    IApplicationDbContext dbContext,
    IDiplomaQueries diplomaQueries,
    IDiplomaDocumentQueries diplomaDocumentQueries,
    IUserDisplayQueries userDisplayQueries,
    IAdmissionStepQueries admissionStepQueries,
    IArchiveGuard archiveGuard,
    DiplomaLifecycleService diplomaLifecycleService,
    IFileStorageService fileStorageService,
    IOptions<FileStorageOptions> storageOptions) : IDiplomaDocumentService
{
    public async Task<DiplomaDocumentsBundleDto> GetDocumentsAsync(
        Guid diplomaId,
        CancellationToken cancellationToken = default)
    {
        List<DiplomaDocument> documents = await diplomaDocumentQueries.ListForDiplomaReadAsync(
            diplomaId,
            cancellationToken);

        List<DiplomaDocumentDto> studentWork = [];
        DiplomaDocumentDto? supervisorFeedback = null;
        DiplomaDocumentDto? externalReview = null;
        DiplomaDocumentDto? antiPlagiarism = null;

        foreach (DiplomaDocument document in documents)
        {
            DiplomaDocumentDto dto = await MapDocumentAsync(document, cancellationToken);
            switch (document.Kind)
            {
                case DiplomaDocumentKind.StudentWork:
                    studentWork.Add(dto);
                    break;
                case DiplomaDocumentKind.SupervisorFeedback:
                    supervisorFeedback = dto;
                    break;
                case DiplomaDocumentKind.ExternalReview:
                    externalReview = dto;
                    break;
                case DiplomaDocumentKind.AntiPlagiarismReport:
                    antiPlagiarism = dto;
                    break;
            }
        }

        return new DiplomaDocumentsBundleDto(
            studentWork,
            supervisorFeedback,
            externalReview,
            antiPlagiarism);
    }

    public async Task<DiplomaDocumentDto> UploadStudentWorkAsync(
        Guid studentId,
        Guid diplomaId,
        UploadFileContent file,
        CancellationToken cancellationToken = default)
    {
        ValidateUploadFile(file);

        Diploma diploma = await GetWritableDiplomaForStudentAsync(studentId, diplomaId, cancellationToken);
        EnsureStudentCanUploadWork(diploma);

        string folderId = await EnsureDiplomaFolderAsync(diploma, cancellationToken);
        int nextVersion = await diplomaDocumentQueries.GetNextVersionNumberAsync(
            diploma.Id,
            DiplomaDocumentKind.StudentWork,
            cancellationToken);
        string studentFullName = await ResolveStudentFullNameAsync(diploma.StudentId, cancellationToken);
        string fileName = DiplomaDocumentNaming.BuildFileName(
            diploma.DefenceSession.Type,
            DiplomaDocumentKind.StudentWork,
            nextVersion,
            Path.GetExtension(file.FileName),
            studentFullName);

        StoredFileResult stored = await fileStorageService.UploadFileAsync(
            folderId,
            fileName,
            file,
            cancellationToken);

        DiplomaDocument document = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diploma.Id,
            Kind = DiplomaDocumentKind.StudentWork,
            VersionNumber = nextVersion,
            StorageFileId = stored.FileId,
            FileName = stored.FileName,
            MimeType = stored.MimeType,
            SizeBytes = stored.SizeBytes,
            UploadedById = studentId,
            UploadedAt = DateTimeOffset.UtcNow,
        };

        dbContext.DiplomaDocuments.Add(document);
        List<DiplomaAdmissionStepAttempt> attempts = await admissionStepQueries.ListForDiplomaAsync(
            diploma.Id,
            cancellationToken);
        DiplomaTopicVersion? latestTopic = diploma.TopicVersions
            .OrderByDescending(version => version.VersionNumber)
            .FirstOrDefault();

        diploma.LifecycleStatus = diplomaLifecycleService.Recalculate(diploma, latestTopic, attempts);
        diploma.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapDocumentAsync(document, cancellationToken);
    }

    public async Task<DiplomaDocumentDto> UploadCheckpointDocumentAsync(
        Guid uploadedById,
        Guid diplomaId,
        AdmissionStep step,
        Guid admissionStepAttemptId,
        UploadFileContent file,
        CancellationToken cancellationToken = default)
    {
        if (!DiplomaDocumentNaming.RequiresFile(step))
        {
            throw new DomainException("This admission step does not require a document upload.");
        }

        ValidateUploadFile(file);

        Diploma diploma = await diplomaQueries.FindWritableAsync(
            new DiplomaWritableCriteria(diplomaId),
            cancellationToken) ?? throw new DomainException("Diploma not found.");

        archiveGuard.EnsureWritable(diploma.DefenceSession);

        DiplomaAdmissionStepAttempt? attempt = await admissionStepQueries.FindWritableAsync(
            diploma.Id,
            admissionStepAttemptId,
            cancellationToken);

        if (attempt is null)
        {
            throw new DomainException("Admission step attempt not found.");
        }

        if (attempt.Step != step)
        {
            throw new DomainException("Admission step attempt does not match the requested step.");
        }

        DiplomaDocumentKind kind = DiplomaDocumentNaming.ToDocumentKind(step);
        string folderId = await EnsureDiplomaFolderAsync(diploma, cancellationToken);
        Dictionary<Guid, string> participantNames = await userDisplayQueries.LoadFullNamesAsync(
            [diploma.StudentId, uploadedById],
            cancellationToken);
        participantNames.TryGetValue(diploma.StudentId, out string? studentFullName);
        participantNames.TryGetValue(uploadedById, out string? actorFullName);
        string fileName = DiplomaDocumentNaming.BuildFileName(
            diploma.DefenceSession.Type,
            kind,
            attempt.AttemptNumber,
            Path.GetExtension(file.FileName),
            studentFullName,
            actorFullName);

        StoredFileResult stored = await fileStorageService.UploadFileAsync(
            folderId,
            fileName,
            file,
            cancellationToken);

        DiplomaDocument document = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diploma.Id,
            Kind = kind,
            VersionNumber = attempt.AttemptNumber,
            StorageFileId = stored.FileId,
            FileName = stored.FileName,
            MimeType = stored.MimeType,
            SizeBytes = stored.SizeBytes,
            UploadedById = uploadedById,
            UploadedAt = DateTimeOffset.UtcNow,
            AdmissionStepAttemptId = attempt.Id,
        };

        dbContext.DiplomaDocuments.Add(document);
        diploma.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapDocumentAsync(document, cancellationToken);
    }

    public Task<bool> HasStudentWorkAsync(Guid diplomaId, CancellationToken cancellationToken = default)
    {
        return diplomaDocumentQueries.HasStudentWorkAsync(diplomaId, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, DiplomaDocumentDto>> GetLatestStudentWorkByDiplomaIdsAsync(
        IReadOnlyCollection<Guid> diplomaIds,
        CancellationToken cancellationToken = default)
    {
        if (diplomaIds.Count == 0)
        {
            return new Dictionary<Guid, DiplomaDocumentDto>();
        }

        Dictionary<Guid, DiplomaDocument> documents = await diplomaDocumentQueries.GetLatestStudentWorkByDiplomaIdsAsync(
            diplomaIds,
            cancellationToken);

        Dictionary<Guid, DiplomaDocumentDto> result = new(documents.Count);
        foreach (KeyValuePair<Guid, DiplomaDocument> entry in documents)
        {
            result[entry.Key] = await MapDocumentAsync(entry.Value, cancellationToken);
        }

        return result;
    }

    public void ValidateUploadFile(UploadFileContent file)
    {
        FileStorageOptions options = storageOptions.Value;

        if (file.Length <= 0)
        {
            throw new DomainException("Файл порожній.");
        }

        if (file.Length > options.MaxFileSizeBytes)
        {
            throw new DomainException("Файл перевищує максимально дозволений розмір.");
        }

        if (!DiplomaDocumentFormats.TryResolve(file.FileName, file.ContentType, out string extension, out _))
        {
            throw new DomainException("Дозволені формати: PDF, DOCX, ODT.");
        }

        if (!options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new DomainException("Дозволені формати: PDF, DOCX, ODT.");
        }
    }

    private async Task<Diploma> GetWritableDiplomaForStudentAsync(
        Guid studentId,
        Guid diplomaId,
        CancellationToken cancellationToken)
    {
        Diploma? diploma = await diplomaQueries.FindWritableAsync(
            new DiplomaWritableCriteria(
                diplomaId,
                StudentId: studentId,
                IncludeTopicVersions: true),
            cancellationToken);

        if (diploma is null)
        {
            throw new DomainException("Diploma not found.");
        }

        archiveGuard.EnsureWritable(diploma.DefenceSession);
        return diploma;
    }

    private static void EnsureStudentCanUploadWork(Diploma diploma)
    {
        bool hasApprovedTopic = diploma.TopicVersions.Any(version => version.Status == TopicVersionStatus.Approved);
        if (!hasApprovedTopic)
        {
            throw new DomainException("Topic must be approved before uploading work.");
        }

        if (diploma.LifecycleStatus is DiplomaLifecycleStatus.Admitted)
        {
            throw new DomainException("Work cannot be uploaded after admission.");
        }

        if (diploma.LifecycleStatus is not (
            DiplomaLifecycleStatus.ReviewerAssigned
            or DiplomaLifecycleStatus.DocumentsInProgress
            or DiplomaLifecycleStatus.ReadyForAdmission))
        {
            throw new DomainException("Work upload is not available at the current lifecycle stage.");
        }
    }

    private async Task<string> EnsureDiplomaFolderAsync(Diploma diploma, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(diploma.StorageFolderId))
        {
            return diploma.StorageFolderId;
        }

        StudentStorageContext? storageContext = await userDisplayQueries.GetStudentStorageContextAsync(
            diploma.StudentId,
            cancellationToken);

        if (storageContext is null)
        {
            throw new DomainException("Student study group is not assigned.");
        }

        StudyGroup studyGroup = new()
        {
            Id = storageContext.StudyGroupId,
            Name = storageContext.StudyGroupName,
        };

        ApplicationUser student = new()
        {
            Id = storageContext.StudentId,
            FullName = storageContext.StudentFullName,
        };

        IReadOnlyList<string> segments = DiplomaStoragePathBuilder.BuildFolderSegments(
            diploma.DefenceSession,
            studyGroup,
            student);

        string? parentFolderId = null;
        foreach (string segment in segments)
        {
            parentFolderId = await fileStorageService.EnsureFolderAsync(parentFolderId, segment, cancellationToken);
        }

        diploma.StorageFolderId = parentFolderId
                                  ?? throw new InvalidOperationException("Diploma storage folder was not created.");

        diploma.UpdatedAt = DateTimeOffset.UtcNow;
        return diploma.StorageFolderId;
    }

    private async Task<string> ResolveStudentFullNameAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        Dictionary<Guid, string> names = await userDisplayQueries.LoadFullNamesAsync(
            [studentId],
            cancellationToken);

        return names.GetValueOrDefault(studentId, "Nevidomyi Student");
    }

    private async Task<DiplomaDocumentDto> MapDocumentAsync(
        DiplomaDocument document,
        CancellationToken cancellationToken)
    {
        string viewUrl = await fileStorageService.GetViewUrlAsync(document.StorageFileId, cancellationToken);
        return new DiplomaDocumentDto(
            document.Id,
            document.Kind,
            document.VersionNumber,
            document.FileName,
            viewUrl,
            document.SizeBytes,
            document.UploadedAt,
            document.AdmissionStepAttemptId);
    }
}
