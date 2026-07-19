using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class DiplomaCreationServiceTests
{
    private readonly DiplomaCreationService _service = new();

    // TC-DOM-DCR-001
    [Fact]
    public void CreateForStudents_NewStudent_CreatesDiploma()
    {
        var sessionId = Guid.NewGuid();
        var studentId = Guid.NewGuid();

        IReadOnlyList<Diploma> diplomas = _service.CreateForStudents(
            [new DiplomaStudentCandidate(studentId, UserKind.Student)],
            sessionId,
            existingStudentIdsInSession: new HashSet<Guid>());

        Diploma diploma = Assert.Single(diplomas);
        Assert.Equal(sessionId, diploma.DefenceSessionId);
        Assert.Equal(studentId, diploma.StudentId);
        Assert.Equal(SupervisorAssignmentStatus.Pending, diploma.SupervisorAssignmentStatus);
        Assert.Equal(ReviewAssignmentStatus.NotAssigned, diploma.ReviewAssignmentStatus);
        Assert.Equal(DiplomaLifecycleStatus.AwaitingSupervisor, diploma.LifecycleStatus);
        Assert.Equal(DiplomaAdmissionStatus.NotAdmitted, diploma.AdmissionStatus);
    }

    // TC-DOM-DCR-002
    [Fact]
    public void CreateForStudents_ExistingStudent_Skips()
    {
        var studentId = Guid.NewGuid();

        IReadOnlyList<Diploma> diplomas = _service.CreateForStudents(
            [new DiplomaStudentCandidate(studentId, UserKind.Student)],
            Guid.NewGuid(),
            existingStudentIdsInSession: new HashSet<Guid> { studentId });

        Assert.Empty(diplomas);
    }

    // TC-DOM-DCR-003
    [Fact]
    public void CreateForStudents_NonStudent_Throws()
    {
        DomainException exception = Assert.Throws<DomainException>(() =>
            _service.CreateForStudents(
                [new DiplomaStudentCandidate(Guid.NewGuid(), UserKind.Employee)],
                Guid.NewGuid(),
                existingStudentIdsInSession: new HashSet<Guid>()));

        Assert.Contains("not a student", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // TC-DOM-DCR-004
    [Fact]
    public void CreateForStudents_Multiple_CreatesAll()
    {
        var sessionId = Guid.NewGuid();

        IReadOnlyList<Diploma> diplomas = _service.CreateForStudents(
        [
            new DiplomaStudentCandidate(Guid.NewGuid(), UserKind.Student),
            new DiplomaStudentCandidate(Guid.NewGuid(), UserKind.Student),
        ],
            sessionId,
            existingStudentIdsInSession: new HashSet<Guid>());

        Assert.Equal(2, diplomas.Count);
        Assert.All(diplomas, diploma => Assert.Equal(sessionId, diploma.DefenceSessionId));
    }
}
