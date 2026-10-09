namespace DiplomaManagementSystem.Domain.Entities;

public sealed class DefenceSessionMilestone
{
    public Guid Id { get; set; }

    public Guid DefenceSessionId { get; set; }

    public DefenceSession DefenceSession { get; set; } = null!;

    public int Ordinal { get; set; }

    public DateOnly DueDate { get; set; }

    public int ExpectedPercent { get; set; }

    public ICollection<DiplomaMilestoneProgress> ProgressEntries { get; set; } = [];
}
