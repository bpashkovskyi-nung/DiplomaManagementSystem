namespace DiplomaManagementSystem.Web.Areas.Admin.Models;

public sealed class EmployeeWorkloadLimitsViewModel
{
    public Guid DefenceSessionId { get; set; }

    public string SessionLabel { get; set; } = string.Empty;

    public IReadOnlyList<EmployeeWorkloadLimitRowViewModel> Rows { get; set; } = [];
}

public sealed class EmployeeWorkloadLimitRowViewModel
{
    public Guid EmployeeId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int? MaxSupervisorStudents { get; set; }

    public int? MaxReviewerStudents { get; set; }

    public int ConfirmedSupervisorCount { get; set; }

    public int ReviewerAssignmentCount { get; set; }
}

public sealed class SetEmployeeWorkloadLimitFormViewModel
{
    public Guid DefenceSessionId { get; set; }

    public Guid EmployeeId { get; set; }

    public int? MaxSupervisorStudents { get; set; }

    public int? MaxReviewerStudents { get; set; }
}
