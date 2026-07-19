namespace DiplomaManagementSystem.Domain.Entities;

public sealed class DiplomaMilestoneProgress
{
    public Guid Id { get; set; }

    public Guid DiplomaId { get; set; }

    public Diploma Diploma { get; set; } = null!;

    public Guid MilestoneId { get; set; }

    public DefenceSessionMilestone Milestone { get; set; } = null!;

    public int ActualPercent { get; set; }

    public Guid RecordedByUserId { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
}
