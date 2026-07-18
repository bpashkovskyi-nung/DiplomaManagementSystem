using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain.Entities;

public sealed class ExaminationCommissionParticipant
{
    public Guid Id { get; set; }

    public Guid DefenceSessionId { get; set; }

    public DefenceSession DefenceSession { get; set; } = null!;

    public ExaminationCommissionRole Role { get; set; }

    public Guid? EmployeeId { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
