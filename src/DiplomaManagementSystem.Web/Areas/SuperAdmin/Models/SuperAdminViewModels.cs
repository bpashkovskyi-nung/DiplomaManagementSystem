using System.ComponentModel.DataAnnotations;
using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport;

using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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
    public Guid FacultyId { get; init; }

    public string FacultyName { get; init; } = string.Empty;

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

    public int SpecialtyCount { get; init; }

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

    public IReadOnlyList<FacultyOptionViewModel> FacultyOptions { get; set; } = [];

    public IReadOnlyList<SpecialtyListItemViewModel> Specialties { get; set; } = [];

    [ValidateNever]
    public SpecialtyFormViewModel NewSpecialty { get; set; } = new();

    public IReadOnlyList<DepartmentAdminListItemViewModel> Admins { get; set; } = [];

    public IReadOnlyList<DepartmentEmployeeOptionViewModel> EmployeeOptions { get; set; } = [];

    public Guid? AssignUserId { get; set; }

    public string FacultyName { get; set; } = string.Empty;
}

public sealed class SpecialtyListItemViewModel
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public int StudyGroupCount { get; init; }
}

public sealed class SpecialtyFormViewModel
{
    public Guid? Id { get; set; }

    public Guid DepartmentId { get; set; }

    [Required(ErrorMessage = "Код обов'язковий.")]
    [MaxLength(64)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Назва обов'язкова.")]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;
}

public sealed class SuperAdminHomeViewModel
{
    public int FacultyCount { get; init; }
}

public sealed class DepartmentEmployeeOptionViewModel
{
    public Guid Id { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
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
