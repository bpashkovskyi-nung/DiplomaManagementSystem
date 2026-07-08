namespace DiplomaManagementSystem.Domain.Entities;

public sealed class Department
{
    public Guid Id { get; set; }

    public Guid FacultyId { get; set; }

    public Faculty? Faculty { get; set; }

    public string Name { get; set; } = string.Empty;

    public string SpecialtyCode { get; set; } = string.Empty;

    public string SpecialtyName { get; set; } = string.Empty;

    public string StudyForm { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<DefenceSession> DefenceSessions { get; set; } = [];

    public ICollection<DepartmentEmployee> Employees { get; set; } = [];

    public ICollection<DepartmentAdminAssignment> AdminAssignments { get; set; } = [];
}
