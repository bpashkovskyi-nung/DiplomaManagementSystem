using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport.Dtos;

namespace DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport.Contracts;

public interface IOrganizationStructureImportService
{
    Task<OrganizationStructureImportResultDto> ImportAsync(
        Stream jsonStream,
        OrganizationStructureImportMode mode,
        CancellationToken cancellationToken = default);
}
