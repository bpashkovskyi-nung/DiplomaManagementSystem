using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Import.Contracts;
using DiplomaManagementSystem.Application.Import.Models;
using DiplomaManagementSystem.Application.Import.Validation;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Services;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Import;

internal sealed class StudentImportService(
    IApplicationDbContext dbContext,
    IImportFileParser parser,
    StudentImportRowValidator validator,
    IUserProvisioningService userProvisioningService,
    DiplomaCreationService diplomaCreationService,
    ImportRowProcessor rowProcessor) : IStudentImportService
{
    public async Task<ImportResult> ImportAsync(
        Guid defenceSessionId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        DefenceSession? session = await dbContext.DefenceSessions
            .AsNoTracking()
            .FirstOrDefaultAsync(defenceSession => defenceSession.Id == defenceSessionId, cancellationToken);

        if (session is null)
        {
            return new ImportResult
            {
                TotalRows = 0,
                Errors = [ImportMessages.DefenceSessionNotFound],
            };
        }

        if (session.Status == DefenceSessionStatus.Archived)
        {
            return new ImportResult
            {
                TotalRows = 0,
                Errors = [ImportMessages.DefenceSessionArchived],
            };
        }

        if (!parser.CanParse(fileName))
        {
            return new ImportResult
            {
                TotalRows = 0,
                Errors = [ImportMessages.UnsupportedFileFormat],
            };
        }

        ImportParseResult<StudentImportRow> parseResult = await parser.ParseStudentsAsync(fileStream, fileName, cancellationToken);
        HashSet<Guid> existingStudentIds = await dbContext.Diplomas
            .Where(diploma => diploma.DefenceSessionId == defenceSessionId)
            .Select(diploma => diploma.StudentId)
            .ToHashSetAsync(cancellationToken);

        await using IApplicationDbTransaction transaction = await dbContext.BeginTransactionAsync(cancellationToken);

        ImportResult result = await rowProcessor.ProcessAsync(
            parseResult.Rows,
            validator,
            async (row, ct) =>
            {
                StudyGroup group = await userProvisioningService.GetOrCreateStudyGroupByNameAsync(
                    defenceSessionId,
                    row.GroupName,
                    ct);
                ApplicationUser user = await userProvisioningService.CreateStudentAsync(
                    row.FullName,
                    row.Email,
                    defenceSessionId,
                    group.Id,
                    ct);

                IReadOnlyList<Diploma> newDiplomas = diplomaCreationService.CreateForStudents(
                    [new DiplomaStudentCandidate(user.Id, UserKind.Student)],
                    defenceSessionId,
                    existingStudentIds);

                dbContext.Diplomas.AddRange(newDiplomas);
                foreach (Diploma diploma in newDiplomas)
                {
                    existingStudentIds.Add(diploma.StudentId);
                }
            },
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ImportResultComposer.Combine(parseResult, result);
    }
}
