using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Import.Contracts;
using DiplomaManagementSystem.Application.Import.Models;
using DiplomaManagementSystem.Application.Import.Validation;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Import;

internal sealed class EmployeeImportService(
    IApplicationDbContext dbContext,
    IImportFileParser parser,
    EmployeeImportRowValidator validator,
    IUserProvisioningService userProvisioningService,
    ImportRowProcessor rowProcessor,
    CurrentDepartmentResolver currentDepartmentResolver) : IEmployeeImportService
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

        Guid departmentId = await currentDepartmentResolver.ResolveRequiredScopedDepartmentIdAsync(cancellationToken);

        ImportParseResult<EmployeeImportRow> parseResult = await parser.ParseEmployeesAsync(fileStream, fileName, cancellationToken);

        await using IApplicationDbTransaction transaction = await dbContext.BeginTransactionAsync(cancellationToken);

        ImportResult result = await rowProcessor.ProcessAsync(
            parseResult.Rows,
            validator,
            (row, ct) => ImportEmployeeRowAsync(row, departmentId, ct),
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return ImportResultComposer.Combine(parseResult, result);
    }

    private async Task ImportEmployeeRowAsync(
        EmployeeImportRow row,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        ApplicationUser user = await userProvisioningService.CreateEmployeeAsync(row.FullName, row.Email, cancellationToken);

        bool alreadyInDepartment = await dbContext.DepartmentEmployees
            .AnyAsync(
                employee => employee.DepartmentId == departmentId && employee.UserId == user.Id,
                cancellationToken);

        if (alreadyInDepartment)
        {
            throw new DomainException($"Викладач {row.Email} уже доданий до цієї кафедри.");
        }

        EmployeeAcademicRank? rank = null;
        if (AcademicRankLabels.TryParse(row.AcademicRankRaw, out EmployeeAcademicRank parsedRank))
        {
            rank = parsedRank;
        }

        string? shortDisplayName = string.IsNullOrWhiteSpace(row.ShortDisplayName)
            ? null
            : row.ShortDisplayName.Trim();

        dbContext.DepartmentEmployees.Add(new DepartmentEmployee
        {
            Id = Guid.NewGuid(),
            DepartmentId = departmentId,
            UserId = user.Id,
            FullName = row.FullName.Trim(),
            AcademicRank = rank,
            ShortDisplayName = shortDisplayName,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });

        user.AcademicRank = rank;
        user.ShortDisplayName = shortDisplayName;
        user.FullName = row.FullName.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
