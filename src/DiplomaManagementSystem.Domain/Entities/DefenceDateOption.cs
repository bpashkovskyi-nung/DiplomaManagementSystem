namespace DiplomaManagementSystem.Domain.Entities;

public sealed class DefenceDateOption
{
    public Guid Id { get; set; }

    public Guid DefenceSessionId { get; set; }

    public DefenceSession DefenceSession { get; set; } = null!;

    public DateOnly Date { get; set; }

    public ICollection<DefenceDatePreference> Preferences { get; set; } = [];
}
