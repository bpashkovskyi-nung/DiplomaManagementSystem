using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain.Entities;

public sealed class DefenceDatePreference
{
    public Guid Id { get; set; }

    public Guid DiplomaId { get; set; }

    public Diploma Diploma { get; set; } = null!;

    public Guid DefenceDateOptionId { get; set; }

    public DefenceDateOption DefenceDateOption { get; set; } = null!;

    public DefenceDateRequesterType RequesterType { get; set; }

    public Guid RequesterUserId { get; set; }

    public DateTimeOffset RequestedAt { get; set; }
}
