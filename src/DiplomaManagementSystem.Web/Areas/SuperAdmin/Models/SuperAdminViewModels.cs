using System.ComponentModel.DataAnnotations;
using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;

public sealed class FacultyListViewModel
{
    public IReadOnlyList<FacultyListItemViewModel> Items { get; init; } = [];
}

public sealed class FacultyListItemViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public int DepartmentCount { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class FacultyFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Назва обов'язкова.")]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;
}

public sealed class DepartmentListViewModel
{
    public Guid? SelectedFacultyId { get; init; }

    public IReadOnlyList<FacultyOptionViewModel> FacultyOptions { get; init; } = [];

    public IReadOnlyList<DepartmentListItemViewModel> Items { get; init; } = [];
}

public sealed class FacultyOptionViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;
}

public sealed class DepartmentListItemViewModel
{
    public Guid Id { get; init; }

    public Guid FacultyId { get; init; }

    public string FacultyName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string SpecialtyCode { get; init; } = string.Empty;

    public string SpecialtyName { get; init; } = string.Empty;

    public string StudyForm { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}

public sealed class DepartmentFormViewModel
{
    public Guid? Id { get; set; }

    [Required]
    public Guid FacultyId { get; set; }

    [Required(ErrorMessage = "Назва обов'язкова.")]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(64)]
    public string SpecialtyCode { get; set; } = string.Empty;

    [MaxLength(256)]
    public string SpecialtyName { get; set; } = string.Empty;

    [MaxLength(128)]
    public string StudyForm { get; set; } = "очної форми навчання";

    public IReadOnlyList<FacultyOptionViewModel> FacultyOptions { get; set; } = [];
}

public sealed class SuperAdminHomeViewModel
{
    public IReadOnlyList<FacultyOverviewViewModel> Faculties { get; init; } = [];

    public IReadOnlyList<DepartmentListItemViewModel> Departments { get; init; } = [];
}

public sealed class FacultyOverviewViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public int DepartmentCount { get; init; }
}

public sealed class DepartmentAdminListViewModel
{
    public Guid? SelectedDepartmentId { get; set; }

    public string? SelectedDepartmentName { get; set; }

    public string AssignEmail { get; set; } = string.Empty;

    public IReadOnlyList<DepartmentOptionViewModel> DepartmentOptions { get; set; } = [];

    public IReadOnlyList<DepartmentAdminListItemViewModel> Items { get; set; } = [];
}

public sealed class DepartmentOptionViewModel
{
    public Guid Id { get; init; }

    public string Label { get; init; } = string.Empty;
}

public sealed class DepartmentAdminListItemViewModel
{
    public Guid AssignmentId { get; init; }

    public Guid UserId { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public DateTimeOffset AssignedAt { get; init; }
}

public sealed class OrganizationImportViewModel
{
    public IFormFile? File { get; set; }

    public OrganizationStructureImportMode Mode { get; set; } = OrganizationStructureImportMode.CreateOnly;

    public OrganizationImportResultViewModel? Result { get; set; }
}

public sealed class OrganizationImportResultViewModel
{
    public int FacultiesCreated { get; init; }

    public int FacultiesUpdated { get; init; }

    public int FacultiesSkipped { get; init; }

    public int DepartmentsCreated { get; init; }

    public int DepartmentsUpdated { get; init; }

    public int DepartmentsSkipped { get; init; }

    public IReadOnlyList<string> Errors { get; init; } = [];
}
