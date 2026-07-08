using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport.Dtos;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;

namespace DiplomaManagementSystem.Web.Mapping;

internal static class SuperAdminViewModelMapper
{
    public static FacultyListItemViewModel Map(FacultyListItemDto dto) =>
        new()
        {
            Id = dto.Id,
            Name = dto.Name,
            IsActive = dto.IsActive,
            DepartmentCount = dto.DepartmentCount,
            CreatedAt = dto.CreatedAt,
        };

    public static FacultyOverviewViewModel MapOverview(FacultyListItemDto dto) =>
        new()
        {
            Id = dto.Id,
            Name = dto.Name,
            IsActive = dto.IsActive,
            DepartmentCount = dto.DepartmentCount,
        };

    public static FacultyOptionViewModel MapOption(FacultyListItemDto dto) =>
        new()
        {
            Id = dto.Id,
            Name = dto.Name,
        };

    public static DepartmentListItemViewModel Map(DepartmentListItemDto dto) =>
        new()
        {
            Id = dto.Id,
            FacultyId = dto.FacultyId,
            FacultyName = dto.FacultyName,
            Name = dto.Name,
            SpecialtyCode = dto.SpecialtyCode,
            SpecialtyName = dto.SpecialtyName,
            StudyForm = dto.StudyForm,
            IsActive = dto.IsActive,
        };

    public static DepartmentOptionViewModel MapDepartmentOption(DepartmentListItemDto dto) =>
        new()
        {
            Id = dto.Id,
            Label = $"{dto.FacultyName} — {dto.Name}",
        };

    public static DepartmentAdminListItemViewModel Map(DepartmentAdminListItemDto dto) =>
        new()
        {
            AssignmentId = dto.AssignmentId,
            UserId = dto.UserId,
            FullName = dto.FullName,
            Email = dto.Email,
            AssignedAt = dto.AssignedAt,
        };

    public static OrganizationImportResultViewModel Map(OrganizationStructureImportResultDto dto) =>
        new()
        {
            FacultiesCreated = dto.FacultiesCreated,
            FacultiesUpdated = dto.FacultiesUpdated,
            FacultiesSkipped = dto.FacultiesSkipped,
            DepartmentsCreated = dto.DepartmentsCreated,
            DepartmentsUpdated = dto.DepartmentsUpdated,
            DepartmentsSkipped = dto.DepartmentsSkipped,
            Errors = dto.Errors,
        };
}
