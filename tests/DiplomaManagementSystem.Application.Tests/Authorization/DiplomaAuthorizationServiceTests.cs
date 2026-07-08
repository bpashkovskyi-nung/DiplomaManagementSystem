using DiplomaManagementSystem.Application.Authorization;
using DiplomaManagementSystem.Application.Tests.Authorization.Fakes;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Application.Tests.Authorization;

public sealed class DiplomaAuthorizationServiceTests
{
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly Guid _diplomaId = Guid.NewGuid();
    private readonly Guid _supervisorId = Guid.NewGuid();
    private readonly Guid _reviewerId = Guid.NewGuid();
    private readonly Guid _secretaryId = Guid.NewGuid();
    private readonly Guid _officerId = Guid.NewGuid();

    // TC-APP-AUTH-001
    [Fact]
    public async Task EnsureCanPerform_DiplomaNotFound_Throws()
    {
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries());

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(Guid.NewGuid(), _diplomaId, DiplomaAction.ConfirmSupervisor));

        Assert.Equal(AuthorizationMessages.DiplomaNotFound, exception.Message);
    }

    // TC-APP-AUTH-002
    [Fact]
    public async Task EnsureCanPerform_SessionMismatch_Throws()
    {
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(
                _supervisorId,
                _diplomaId,
                DiplomaAction.ConfirmSupervisor,
                expectedSessionId: Guid.NewGuid()));

        Assert.Equal(AuthorizationMessages.SessionMismatch, exception.Message);
    }

    // TC-APP-AUTH-003
    [Fact]
    public async Task EnsureCanPerform_ArchivedSession_Throws()
    {
        Diploma diploma = CreateActiveDiploma();
        diploma.DefenceSession = CreateSession(DefenceSessionStatus.Archived);
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(_supervisorId, _diplomaId, DiplomaAction.ConfirmSupervisor));

        Assert.Equal(AuthorizationMessages.SessionArchived, exception.Message);
    }

    // TC-APP-AUTH-004
    [Fact]
    public async Task EnsureCanPerformOnTopicVersion_NotFound_Throws()
    {
        DiplomaAuthorizationService service = CreateService(
            new FakeDiplomaQueries(),
            new FakeTopicVersionQueries());

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformOnTopicVersionAsync(
                _supervisorId,
                Guid.NewGuid(),
                DiplomaAction.ApproveTopicAsSupervisor));

        Assert.Equal(AuthorizationMessages.TopicVersionNotFound, exception.Message);
    }

    // TC-APP-AUTH-005
    [Fact]
    public async Task EnsureCanPerform_UnsupportedAction_Throws()
    {
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        DiplomaAction invalidAction = (DiplomaAction)999;

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(_supervisorId, _diplomaId, invalidAction));

        Assert.Equal(AuthorizationMessages.UnsupportedAction, exception.Message);
    }

    // TC-APP-AUTH-010
    [Fact]
    public async Task SupervisorAction_AssignedSupervisor_Succeeds()
    {
        Diploma diploma = CreateActiveDiploma(supervisorId: _supervisorId);
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        await service.EnsureCanPerformAsync(_supervisorId, _diplomaId, DiplomaAction.ConfirmSupervisor);
    }

    // TC-APP-AUTH-011
    [Fact]
    public async Task SupervisorAction_WrongUser_Throws()
    {
        Diploma diploma = CreateActiveDiploma(supervisorId: _supervisorId);
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(Guid.NewGuid(), _diplomaId, DiplomaAction.ConfirmSupervisor));

        Assert.Equal(AuthorizationMessages.NotSupervisor, exception.Message);
    }

    // TC-APP-AUTH-012
    [Fact]
    public async Task RejectSupervisor_WrongUser_Throws()
    {
        Diploma diploma = CreateActiveDiploma(supervisorId: _supervisorId);
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(Guid.NewGuid(), _diplomaId, DiplomaAction.RejectSupervisor));

        Assert.Equal(AuthorizationMessages.NotSupervisor, exception.Message);
    }

    // TC-APP-AUTH-013
    [Fact]
    public async Task CompleteSupervisorCheckpoint_Supervisor_Succeeds()
    {
        Diploma diploma = CreateActiveDiploma(supervisorId: _supervisorId);
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        await service.EnsureCanPerformAsync(_supervisorId, _diplomaId, DiplomaAction.CompleteSupervisorCheckpoint);
    }

    // TC-APP-AUTH-020
    [Fact]
    public async Task CompleteExternalReview_AssignedReviewer_Succeeds()
    {
        Diploma diploma = CreateActiveDiploma(reviewerId: _reviewerId);
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        await service.EnsureCanPerformAsync(_reviewerId, _diplomaId, DiplomaAction.CompleteExternalReview);
    }

    // TC-APP-AUTH-021
    [Fact]
    public async Task CompleteExternalReview_WrongUser_Throws()
    {
        Diploma diploma = CreateActiveDiploma(reviewerId: _reviewerId);
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(Guid.NewGuid(), _diplomaId, DiplomaAction.CompleteExternalReview));

        Assert.Equal(AuthorizationMessages.NotReviewer, exception.Message);
    }

    // TC-APP-AUTH-030
    [Fact]
    public async Task CompleteAntiPlagiarism_WithRole_Succeeds()
    {
        FakeAnnualRoleQueries roles = new();
        roles.Roles.Add((_officerId, _sessionId, AnnualRoleType.AntiPlagiarismOfficer));
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma), annualRoleQueries: roles);

        await service.EnsureCanPerformAsync(_officerId, _diplomaId, DiplomaAction.CompleteAntiPlagiarism);
    }

    // TC-APP-AUTH-031
    [Fact]
    public async Task CompleteAntiPlagiarism_NoRole_Throws()
    {
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(_officerId, _diplomaId, DiplomaAction.CompleteAntiPlagiarism));

        Assert.Equal(AuthorizationMessages.MissingSessionRole, exception.Message);
    }

    // TC-APP-AUTH-032
    [Fact]
    public async Task CompleteFormattingReview_WithRole_Succeeds()
    {
        FakeAnnualRoleQueries roles = new();
        roles.Roles.Add((_officerId, _sessionId, AnnualRoleType.FormattingReviewer));
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma), annualRoleQueries: roles);

        await service.EnsureCanPerformAsync(_officerId, _diplomaId, DiplomaAction.CompleteFormattingReview);
    }

    // TC-APP-AUTH-033
    [Fact]
    public async Task CompleteFormattingReview_NoRole_Throws()
    {
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(_officerId, _diplomaId, DiplomaAction.CompleteFormattingReview));

        Assert.Equal(AuthorizationMessages.MissingSessionRole, exception.Message);
    }

    // TC-APP-AUTH-034
    [Fact]
    public async Task DepartmentHeadTopicAction_WithRole_Succeeds()
    {
        Guid headId = Guid.NewGuid();
        FakeAnnualRoleQueries roles = new();
        roles.Roles.Add((headId, _sessionId, AnnualRoleType.DepartmentHead));
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma), annualRoleQueries: roles);

        await service.EnsureCanPerformAsync(headId, _diplomaId, DiplomaAction.ApproveTopicAsDepartmentHead);
    }

    // TC-APP-AUTH-035
    [Fact]
    public async Task DepartmentHeadTopicAction_NoRole_Throws()
    {
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(Guid.NewGuid(), _diplomaId, DiplomaAction.ApproveTopicAsDepartmentHead));

        Assert.Equal(AuthorizationMessages.NotDepartmentHead, exception.Message);
    }

    // TC-APP-AUTH-036
    [Fact]
    public async Task RejectTopicAsDepartmentHead_OnVersion_Succeeds()
    {
        Guid headId = Guid.NewGuid();
        FakeAnnualRoleQueries roles = new();
        roles.Roles.Add((headId, _sessionId, AnnualRoleType.DepartmentHead));
        Diploma diploma = CreateActiveDiploma();
        DiplomaTopicVersion version = CreateTopicVersion(diploma);
        DiplomaAuthorizationService service = CreateService(
            new FakeDiplomaQueries(diploma),
            new FakeTopicVersionQueries(version),
            roles);

        await service.EnsureCanPerformOnTopicVersionAsync(
            headId,
            version.Id,
            DiplomaAction.RejectTopicAsDepartmentHead);
    }

    // TC-APP-AUTH-040
    [Fact]
    public async Task SecretaryAction_WithAccess_Succeeds()
    {
        FakeAnnualRoleQueries roles = new() { CanAccessAsSecretary = true };
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma), annualRoleQueries: roles);

        await service.EnsureCanPerformAsync(_secretaryId, _diplomaId, DiplomaAction.AssignReviewer);
    }

    // TC-APP-AUTH-041
    [Fact]
    public async Task SecretaryAction_NoAccess_Throws()
    {
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformAsync(_secretaryId, _diplomaId, DiplomaAction.AssignReviewer));

        Assert.Equal(AuthorizationMessages.NotSecretaryForSession, exception.Message);
    }

    // TC-APP-AUTH-042
    [Fact]
    public async Task AdmitDiploma_WithAccess_Succeeds()
    {
        FakeAnnualRoleQueries roles = new() { CanAccessAsSecretary = true };
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma), annualRoleQueries: roles);

        await service.EnsureCanPerformAsync(_secretaryId, _diplomaId, DiplomaAction.AdmitDiploma);
    }

    // TC-APP-AUTH-043
    [Fact]
    public async Task OverrideSupervisor_WithAccess_Succeeds()
    {
        FakeAnnualRoleQueries roles = new() { CanAccessAsSecretary = true };
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma), annualRoleQueries: roles);

        await service.EnsureCanPerformAsync(_secretaryId, _diplomaId, DiplomaAction.OverrideSupervisor);
    }

    // TC-APP-AUTH-044
    [Fact]
    public async Task AddSecretaryComment_WithAccess_Succeeds()
    {
        FakeAnnualRoleQueries roles = new() { CanAccessAsSecretary = true };
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma), annualRoleQueries: roles);

        await service.EnsureCanPerformAsync(_secretaryId, _diplomaId, DiplomaAction.AddSecretaryComment);
    }

    // TC-APP-AUTH-045
    [Fact]
    public async Task OverrideAdmissionStep_WithAccess_Succeeds()
    {
        FakeAnnualRoleQueries roles = new() { CanAccessAsSecretary = true };
        Diploma diploma = CreateActiveDiploma();
        DiplomaAuthorizationService service = CreateService(new FakeDiplomaQueries(diploma), annualRoleQueries: roles);

        await service.EnsureCanPerformAsync(_secretaryId, _diplomaId, DiplomaAction.OverrideAdmissionStep);
    }

    // TC-APP-AUTH-050
    [Fact]
    public async Task ApproveTopicAsSupervisor_OnVersion_Succeeds()
    {
        Diploma diploma = CreateActiveDiploma(supervisorId: _supervisorId);
        DiplomaTopicVersion version = CreateTopicVersion(diploma);
        DiplomaAuthorizationService service = CreateService(
            new FakeDiplomaQueries(diploma),
            new FakeTopicVersionQueries(version));

        await service.EnsureCanPerformOnTopicVersionAsync(
            _supervisorId,
            version.Id,
            DiplomaAction.ApproveTopicAsSupervisor);
    }

    // TC-APP-AUTH-051
    [Fact]
    public async Task ApproveTopicAsSupervisor_WrongUser_Throws()
    {
        Diploma diploma = CreateActiveDiploma(supervisorId: _supervisorId);
        DiplomaTopicVersion version = CreateTopicVersion(diploma);
        DiplomaAuthorizationService service = CreateService(
            new FakeDiplomaQueries(diploma),
            new FakeTopicVersionQueries(version));

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            service.EnsureCanPerformOnTopicVersionAsync(
                Guid.NewGuid(),
                version.Id,
                DiplomaAction.ApproveTopicAsSupervisor));

        Assert.Equal(AuthorizationMessages.NotSupervisor, exception.Message);
    }

    // TC-APP-AUTH-052
    [Fact]
    public async Task RejectTopicAsSupervisor_OnVersion_Succeeds()
    {
        Diploma diploma = CreateActiveDiploma(supervisorId: _supervisorId);
        DiplomaTopicVersion version = CreateTopicVersion(diploma);
        DiplomaAuthorizationService service = CreateService(
            new FakeDiplomaQueries(diploma),
            new FakeTopicVersionQueries(version));

        await service.EnsureCanPerformOnTopicVersionAsync(
            _supervisorId,
            version.Id,
            DiplomaAction.RejectTopicAsSupervisor);
    }

    private DiplomaAuthorizationService CreateService(
        FakeDiplomaQueries diplomaQueries,
        FakeTopicVersionQueries? topicVersionQueries = null,
        FakeAnnualRoleQueries? annualRoleQueries = null,
        FakeDepartmentAuthorizationService? departmentAuthorization = null) =>
        new(
            diplomaQueries,
            topicVersionQueries ?? new FakeTopicVersionQueries(),
            annualRoleQueries ?? new FakeAnnualRoleQueries(),
            departmentAuthorization ?? new FakeDepartmentAuthorizationService());

    private Diploma CreateActiveDiploma(Guid? supervisorId = null, Guid? reviewerId = null)
    {
        Diploma diploma = new()
        {
            Id = _diplomaId,
            DefenceSessionId = _sessionId,
            SupervisorId = supervisorId,
            ReviewerId = reviewerId,
            DefenceSession = CreateSession(DefenceSessionStatus.Active),
        };

        return diploma;
    }

    private static DefenceSession CreateSession(DefenceSessionStatus status) => new()
    {
        Status = status,
    };

    private static DiplomaTopicVersion CreateTopicVersion(Diploma diploma)
    {
        DiplomaTopicVersion version = new()
        {
            Id = Guid.NewGuid(),
            DiplomaId = diploma.Id,
            Diploma = diploma,
            VersionNumber = 1,
            Title = "Тема",
            Status = TopicVersionStatus.PendingSupervisor,
            SubmittedAt = DateTimeOffset.UtcNow,
        };

        return version;
    }
}
