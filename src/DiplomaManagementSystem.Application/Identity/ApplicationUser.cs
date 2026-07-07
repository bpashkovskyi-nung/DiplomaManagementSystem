using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

using Microsoft.AspNetCore.Identity;

namespace DiplomaManagementSystem.Application.Identity;
public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;

    public EmployeeAcademicRank? AcademicRank { get; set; }

    public string? ShortDisplayName { get; set; }

    public UserKind UserKind { get; set; }

    public Guid? StudyGroupId { get; set; }

    public StudyGroup? StudyGroup { get; set; }

    public Guid? DefenceSessionId { get; set; }

    public DefenceSession? DefenceSession { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
