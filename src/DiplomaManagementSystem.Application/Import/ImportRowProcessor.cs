using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Import.Models;
using DiplomaManagementSystem.Application.Security;
using DiplomaManagementSystem.Domain.Exceptions;

using FluentValidation;

namespace DiplomaManagementSystem.Application.Import;

internal sealed class ImportRowProcessor(EmailDomainValidator emailDomainValidator)
{
    public async Task<ImportResult> ProcessAsync<T>(
        IReadOnlyList<T> rows,
        IValidator<T> validator,
        Func<T, CancellationToken, Task> importRowAsync,
        CancellationToken cancellationToken = default)
        where T : IImportRow
    {
        List<string> errors = [];
        int imported = 0;
        int skipped = 0;

        foreach ((T row, int index) in rows.Select((row, index) => (row, index)))
        {
            int rowNumber = index + 1;
            FluentValidation.Results.ValidationResult validation = await validator.ValidateAsync(row, cancellationToken);
            if (!validation.IsValid)
            {
                skipped++;
                errors.Add(ImportMessages.RowValidationFailed(
                    rowNumber,
                    string.Join("; ", validation.Errors.Select(error => error.ErrorMessage))));
                continue;
            }

            if (!emailDomainValidator.IsAllowed(row.Email))
            {
                skipped++;
                errors.Add(ImportMessages.EmailDomainNotAllowed(rowNumber, row.Email));
                continue;
            }

            try
            {
                await importRowAsync(row, cancellationToken);
                imported++;
            }
            catch (DomainException ex) when (UserProvisioningMessages.IsEmailAlreadyInUse(ex))
            {
                skipped++;
                errors.Add(ImportMessages.DuplicateEmail(rowNumber, row.Email));
            }
            catch (Exception ex)
            {
                skipped++;
                errors.Add(ImportMessages.RowFailed(rowNumber, ex.Message));
            }
        }

        return new ImportResult
        {
            TotalRows = rows.Count,
            ImportedCount = imported,
            SkippedCount = skipped,
            Errors = errors,
        };
    }
}
