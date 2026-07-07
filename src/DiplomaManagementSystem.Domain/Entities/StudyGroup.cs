namespace DiplomaManagementSystem.Domain.Entities;

public sealed class StudyGroup
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int? Course { get; set; }

    public Guid DefenceSessionId { get; set; }

    public DefenceSession? DefenceSession { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
