namespace DiplomaManagementSystem.Domain.Entities;

public sealed class Faculty
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Department> Departments { get; set; } = [];
}
