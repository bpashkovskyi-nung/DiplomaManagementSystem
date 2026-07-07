namespace DiplomaManagementSystem.Application.Import.Models;

public sealed record EmployeeImportRow(
    string FullName,
    string Email,
    string? AcademicRankRaw = null,
    string? ShortDisplayName = null) : IImportRow;
