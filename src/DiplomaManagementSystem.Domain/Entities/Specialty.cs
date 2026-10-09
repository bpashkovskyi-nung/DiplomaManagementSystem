namespace DiplomaManagementSystem.Domain.Entities;

public sealed class Specialty
{
    public Guid Id { get; set; }

    public Guid DepartmentId { get; set; }

    public Department? Department { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<StudyGroup> StudyGroups { get; set; } = [];
}
