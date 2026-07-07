using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Web.Areas.Admin.Models;

public sealed class EmployeeListViewModel
{
    public IReadOnlyList<EmployeeListItemViewModel> Items { get; set; } = [];
}

public sealed class EmployeeListItemViewModel
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string AcademicRankDisplay { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class EmployeeFormViewModel
{
    public Guid? Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public EmployeeAcademicRank? AcademicRank { get; set; }

    public string? ShortDisplayName { get; set; }
}

public sealed class EmployeeDetailsViewModel
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string AcademicRankDisplay { get; set; } = string.Empty;

    public bool HasAssignments { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public bool CanDelete => !HasAssignments;
}

public sealed class EmployeeDeleteViewModel
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool HasAssignments { get; set; }

    public bool CanDelete => !HasAssignments;
}
