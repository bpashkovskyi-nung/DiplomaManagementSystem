using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain.Entities;

public sealed class DepartmentEmployee
{
    public Guid Id { get; set; }

    public Guid DepartmentId { get; set; }

    public Department? Department { get; set; }

    public Guid UserId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public EmployeeAcademicRank? AcademicRank { get; set; }

    public string? ShortDisplayName { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }
}
