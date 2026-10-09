using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Web.Areas.Admin.Models;

public sealed class AnnualRolesViewModel
{
    public Guid DefenceSessionId { get; set; }

    public string SessionLabel { get; set; } = string.Empty;

    public IReadOnlyList<AnnualRoleSlotViewModel> Roles { get; set; } = [];

    public IReadOnlyList<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Employees { get; set; } = [];

    public ExaminationCommissionFormViewModel Commission { get; set; } = new();

    public IReadOnlyList<CommissionEmployeeOptionViewModel> CommissionEmployees { get; set; } = [];
}

public sealed class AnnualRoleSlotViewModel
{
    public AnnualRoleType RoleType { get; set; }

    public string RoleDisplay { get; set; } = string.Empty;

    public Guid? AssignedEmployeeId { get; set; }

    public string? AssignedEmployeeName { get; set; }

    public Guid SelectedEmployeeId { get; set; }
}

public sealed class AssignAnnualRoleFormViewModel
{
    public Guid DefenceSessionId { get; set; }

    public AnnualRoleType RoleType { get; set; }

    public Guid EmployeeId { get; set; }
}

public sealed class CommissionEmployeeOptionViewModel
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string? Position { get; set; }
}

public sealed class ExaminationCommissionFormViewModel
{
    public Guid DefenceSessionId { get; set; }

    public ExaminationCommissionParticipantFormViewModel Chair { get; set; } = new();

    public List<ExaminationCommissionParticipantFormViewModel> Members { get; set; } = [];
}

public sealed class ExaminationCommissionParticipantFormViewModel
{
    public bool IsExternal { get; set; }

    public Guid? EmployeeId { get; set; }

    public string? FullName { get; set; }

    public string? Position { get; set; }
}

public sealed class CommissionParticipantFieldsModel
{
    public string Prefix { get; set; } = string.Empty;

    public ExaminationCommissionParticipantFormViewModel Participant { get; set; } = new();

    public IReadOnlyList<CommissionEmployeeOptionViewModel> Employees { get; set; } = [];
}
