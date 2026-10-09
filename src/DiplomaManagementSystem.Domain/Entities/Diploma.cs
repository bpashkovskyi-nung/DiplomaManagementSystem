using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain.Entities;

public sealed class Diploma
{
    public Guid Id { get; set; }

    public Guid DefenceSessionId { get; set; }

    public DefenceSession DefenceSession { get; set; } = null!;

    public Guid StudentId { get; set; }

    public Guid? SupervisorId { get; set; }

    public Guid? ReviewerId { get; set; }

    public SupervisorAssignmentStatus SupervisorAssignmentStatus { get; set; }

    public ReviewAssignmentStatus ReviewAssignmentStatus { get; set; }

    public DiplomaLifecycleStatus LifecycleStatus { get; set; }

    public DiplomaAdmissionStatus AdmissionStatus { get; set; }

    public AdmissionStep? CurrentAdmissionStep { get; set; }

    public DateOnly? DefenceDate { get; set; }

    public long RowVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public string? StorageFolderId { get; set; }

    public ICollection<DiplomaTopicVersion> TopicVersions { get; set; } = [];

    public ICollection<DiplomaDocument> Documents { get; set; } = [];

    public ICollection<DiplomaAdmissionStepAttempt> AdmissionStepAttempts { get; set; } = [];

    public ICollection<DiplomaComment> Comments { get; set; } = [];

    public ICollection<DiplomaMilestoneProgress> MilestoneProgressEntries { get; set; } = [];

    public DefenceDatePreference? DefenceDatePreference { get; set; }
}
