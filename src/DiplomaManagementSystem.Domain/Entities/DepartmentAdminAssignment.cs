namespace DiplomaManagementSystem.Domain.Entities;

public sealed class DepartmentAdminAssignment
{
    public Guid Id { get; set; }

    public Guid DepartmentId { get; set; }

    public Department? Department { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset AssignedAt { get; set; }
}
