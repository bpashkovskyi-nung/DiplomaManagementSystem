using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain.Entities;

public sealed class DefenceSession
{
    public Guid Id { get; set; }

    public int Year { get; set; }

    public DefenceSessionType Type { get; set; }

    public int? Semester { get; set; }

    public DefenceSessionStatus Status { get; set; }

    public Guid DepartmentId { get; set; }

    public Department? Department { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ArchivedAt { get; set; }

    public ICollection<StudyGroup> StudyGroups { get; set; } = [];

    public ICollection<Diploma> Diplomas { get; set; } = [];

    public ICollection<AnnualRoleAssignment> RoleAssignments { get; set; } = [];

    public ICollection<ExaminationCommissionParticipant> ExaminationCommissionParticipants { get; set; } = [];
}
