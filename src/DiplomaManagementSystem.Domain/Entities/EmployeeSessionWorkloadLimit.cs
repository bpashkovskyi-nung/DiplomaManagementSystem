namespace DiplomaManagementSystem.Domain.Entities;

public sealed class EmployeeSessionWorkloadLimit
{
    public Guid Id { get; set; }

    public Guid DefenceSessionId { get; set; }

    public DefenceSession DefenceSession { get; set; } = null!;

    public Guid EmployeeId { get; set; }

    public int? MaxSupervisorStudents { get; set; }

    public int? MaxReviewerStudents { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
