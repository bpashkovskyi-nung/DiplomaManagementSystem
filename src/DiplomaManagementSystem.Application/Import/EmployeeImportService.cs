using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Import.Contracts;
using DiplomaManagementSystem.Application.Import.Models;
using DiplomaManagementSystem.Application.Import.Validation;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Import;

internal sealed class EmployeeImportService(
    IApplicationDbContext dbContext,
    IImportFileParser parser,
    EmployeeImportRowValidator validator,
    IUserProvisioningService userProvisioningService,
    ImportRowProcessor rowProcessor) : IEmployeeImportService
{
    public async Task<ImportResult> ImportAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default)
    {
        if (!parser.CanParse(fileName))
        {
            return new ImportResult
            {
                TotalRows = 0,
                Errors = [ImportMessages.UnsupportedFileFormat],
            };
        }

        ImportParseResult<EmployeeImportRow> parseResult = await parser.ParseEmployeesAsync(fileStream, fileName, cancellationToken);

        await using IApplicationDbTransaction transaction = await dbContext.BeginTransactionAsync(cancellationToken);

        ImportResult result = await rowProcessor.ProcessAsync(
            parseResult.Rows,
            validator,
            ImportEmployeeRowAsync,
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return ImportResultComposer.Combine(parseResult, result);
    }

    private async Task ImportEmployeeRowAsync(EmployeeImportRow row, CancellationToken cancellationToken)
    {
        ApplicationUser user = await userProvisioningService.CreateEmployeeAsync(row.FullName, row.Email, cancellationToken);

        if (AcademicRankLabels.TryParse(row.AcademicRankRaw, out EmployeeAcademicRank rank))
        {
            user.AcademicRank = rank;
        }

        user.ShortDisplayName = string.IsNullOrWhiteSpace(row.ShortDisplayName)
            ? null
            : row.ShortDisplayName.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
