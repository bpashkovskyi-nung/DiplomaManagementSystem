namespace DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport.Dtos;

public sealed record OrganizationStructureImportResultDto(
    int FacultiesCreated,
    int FacultiesUpdated,
    int FacultiesSkipped,
    int DepartmentsCreated,
    int DepartmentsUpdated,
    int DepartmentsSkipped,
    IReadOnlyList<string> Errors);
