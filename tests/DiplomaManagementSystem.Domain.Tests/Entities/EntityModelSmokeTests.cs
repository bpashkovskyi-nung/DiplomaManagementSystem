using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Domain.Tests.Entities;

public sealed class EntityModelSmokeTests
{
    [Fact]
    public void Diploma_CanSetNavigationProperties()
    {
        DefenceSession session = new() { Id = Guid.NewGuid() };
        DiplomaAdmissionStepAttempt attempt = new() { Id = Guid.NewGuid() };

        Diploma diploma = new()
        {
            Id = Guid.NewGuid(),
            DefenceSession = session,
            DefenceSessionId = session.Id,
            Documents = [new DiplomaDocument { Id = Guid.NewGuid(), DiplomaId = Guid.NewGuid() }],
            Comments = [new DiplomaComment { Id = Guid.NewGuid(), DiplomaId = Guid.NewGuid(), Body = "note" }],
            AdmissionStepAttempts = [attempt],
            TopicVersions = [new DiplomaTopicVersion { Id = Guid.NewGuid(), Title = "Topic" }],
            StorageFolderId = "folder",
        };

        Assert.Same(session, diploma.DefenceSession);
        Assert.Single(diploma.Documents);
        Assert.Single(diploma.Comments);
    }

    [Fact]
    public void StudyGroup_CanSetDefenceSessionNavigation()
    {
        DefenceSession session = new() { Id = Guid.NewGuid() };
        StudyGroup group = new()
        {
            Id = Guid.NewGuid(),
            Name = "КН-41",
            DefenceSessionId = session.Id,
            DefenceSession = session,
        };

        Assert.Same(session, group.DefenceSession);
    }

    [Fact]
    public void AuditLog_CanSetDefenceSessionNavigation()
    {
        DefenceSession session = new() { Id = Guid.NewGuid() };
        AuditLog auditLog = new()
        {
            Id = Guid.NewGuid(),
            EntityType = "Diploma",
            EntityId = Guid.NewGuid(),
            Action = "Update",
            PerformedById = Guid.NewGuid(),
            DefenceSessionId = session.Id,
            DefenceSession = session,
        };

        Assert.Same(session, auditLog.DefenceSession);
    }

    [Fact]
    public void DiplomaDocument_CanSetAdmissionStepAttemptNavigation()
    {
        DiplomaAdmissionStepAttempt attempt = new() { Id = Guid.NewGuid() };
        DiplomaDocument document = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = Guid.NewGuid(),
            Kind = DiplomaDocumentKind.StudentWork,
            AdmissionStepAttemptId = attempt.Id,
            AdmissionStepAttempt = attempt,
            StorageFileId = "file",
            FileName = "work.pdf",
            MimeType = "application/pdf",
        };

        Assert.Same(attempt, document.AdmissionStepAttempt);
    }

    [Fact]
    public void ExaminationCommissionParticipant_CanSetDefenceSessionNavigation()
    {
        DefenceSession session = new() { Id = Guid.NewGuid() };
        ExaminationCommissionParticipant participant = new()
        {
            Id = Guid.NewGuid(),
            DefenceSessionId = session.Id,
            DefenceSession = session,
            Role = ExaminationCommissionRole.Chair,
            FullName = "Тест",
            Position = "проф.",
        };
        session.ExaminationCommissionParticipants.Add(participant);

        Assert.Same(session, participant.DefenceSession);
        Assert.Contains(participant, session.ExaminationCommissionParticipants);
    }
}
