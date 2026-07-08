using DiplomaManagementSystem.Application.Authorization.Contracts;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;

namespace DiplomaManagementSystem.Application.Authorization;

internal sealed class DiplomaAuthorizationService(
    IDiplomaQueries diplomaQueries,
    ITopicVersionQueries topicVersionQueries,
    IAnnualRoleQueries annualRoleQueries,
    IDepartmentAuthorizationService departmentAuthorization) : IDiplomaAuthorizationService
{
    public async Task EnsureCanPerformAsync(
        Guid userId,
        Guid diplomaId,
        DiplomaAction action,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanPerformAsync(userId, diplomaId, action, expectedSessionId: null, cancellationToken);
    }

    public async Task EnsureCanPerformAsync(
        Guid userId,
        Guid diplomaId,
        DiplomaAction action,
        Guid? expectedSessionId,
        CancellationToken cancellationToken = default)
    {
        Diploma? diploma = await diplomaQueries.FindForAuthorizationAsync(diplomaId, cancellationToken);

        if (diploma is null)
        {
            throw new DomainException(AuthorizationMessages.DiplomaNotFound);
        }

        if (expectedSessionId.HasValue && diploma.DefenceSessionId != expectedSessionId.Value)
        {
            throw new DomainException(AuthorizationMessages.SessionMismatch);
        }

        EnsureSessionWritable(diploma.DefenceSession);
        await EnsureDiplomaActionAsync(diploma, userId, action, cancellationToken);
    }

    public async Task EnsureCanPerformOnTopicVersionAsync(
        Guid userId,
        Guid versionId,
        DiplomaAction action,
        CancellationToken cancellationToken = default)
    {
        DiplomaTopicVersion? version = await topicVersionQueries.FindWritableAsync(versionId, cancellationToken);

        if (version is null)
        {
            throw new DomainException(AuthorizationMessages.TopicVersionNotFound);
        }

        EnsureSessionWritable(version.Diploma.DefenceSession);
        await EnsureTopicVersionActionAsync(version, userId, action, cancellationToken);
    }

    private async Task EnsureDiplomaActionAsync(
        Diploma diploma,
        Guid userId,
        DiplomaAction action,
        CancellationToken cancellationToken)
    {
        switch (action)
        {
            case DiplomaAction.ConfirmSupervisor:
            case DiplomaAction.RejectSupervisor:
            case DiplomaAction.ApproveTopicAsSupervisor:
            case DiplomaAction.RejectTopicAsSupervisor:
            case DiplomaAction.CompleteSupervisorCheckpoint:
                await EnsureSupervisorAsync(diploma, userId, cancellationToken);
                break;

            case DiplomaAction.CompleteExternalReview:
                await EnsureReviewerAsync(diploma, userId, cancellationToken);
                break;

            case DiplomaAction.CompleteAntiPlagiarism:
                await EnsureSessionRoleAsync(
                    userId,
                    diploma.DefenceSessionId,
                    AnnualRoleType.AntiPlagiarismOfficer,
                    cancellationToken);
                break;

            case DiplomaAction.CompleteFormattingReview:
                await EnsureSessionRoleAsync(
                    userId,
                    diploma.DefenceSessionId,
                    AnnualRoleType.FormattingReviewer,
                    cancellationToken);
                break;

            case DiplomaAction.ApproveTopicAsDepartmentHead:
            case DiplomaAction.RejectTopicAsDepartmentHead:
                await EnsureSessionRoleAsync(
                    userId,
                    diploma.DefenceSessionId,
                    AnnualRoleType.DepartmentHead,
                    cancellationToken);
                break;

            case DiplomaAction.AssignReviewer:
            case DiplomaAction.AdmitDiploma:
            case DiplomaAction.OverrideSupervisor:
            case DiplomaAction.AddSecretaryComment:
            case DiplomaAction.OverrideAdmissionStep:
                await EnsureSecretaryForSessionAsync(userId, diploma.DefenceSessionId, cancellationToken);
                break;

            default:
                throw new DomainException(AuthorizationMessages.UnsupportedAction);
        }
    }

    private async Task EnsureSecretaryForSessionAsync(
        Guid userId,
        Guid defenceSessionId,
        CancellationToken cancellationToken)
    {
        bool canAccess = await annualRoleQueries.CanAccessSessionAsSecretaryAsync(
            userId,
            defenceSessionId,
            cancellationToken);

        if (!canAccess)
        {
            throw new DomainException(AuthorizationMessages.NotSecretaryForSession);
        }
    }

    private async Task EnsureTopicVersionActionAsync(
        DiplomaTopicVersion version,
        Guid userId,
        DiplomaAction action,
        CancellationToken cancellationToken)
    {
        switch (action)
        {
            case DiplomaAction.ApproveTopicAsSupervisor:
            case DiplomaAction.RejectTopicAsSupervisor:
                await EnsureSupervisorAsync(version.Diploma, userId, cancellationToken);
                break;

            case DiplomaAction.ApproveTopicAsDepartmentHead:
            case DiplomaAction.RejectTopicAsDepartmentHead:
                await EnsureSessionRoleAsync(
                    userId,
                    version.Diploma.DefenceSessionId,
                    AnnualRoleType.DepartmentHead,
                    cancellationToken);
                break;

            default:
                throw new DomainException(AuthorizationMessages.UnsupportedAction);
        }
    }

    private static void EnsureSessionWritable(DefenceSession session)
    {
        if (session.Status == DefenceSessionStatus.Archived)
        {
            throw new DomainException(AuthorizationMessages.SessionArchived);
        }
    }

    private async Task EnsureSupervisorAsync(Diploma diploma, Guid userId, CancellationToken cancellationToken)
    {
        if (diploma.SupervisorId != userId)
        {
            throw new DomainException(AuthorizationMessages.NotSupervisor);
        }

        await EnsureEmployeeDepartmentMembershipAsync(diploma, userId, cancellationToken);
    }

    private async Task EnsureReviewerAsync(Diploma diploma, Guid userId, CancellationToken cancellationToken)
    {
        if (diploma.ReviewerId != userId)
        {
            throw new DomainException(AuthorizationMessages.NotReviewer);
        }

        await EnsureEmployeeDepartmentMembershipAsync(diploma, userId, cancellationToken);
    }

    private async Task EnsureEmployeeDepartmentMembershipAsync(
        Diploma diploma,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (diploma.DefenceSession?.DepartmentId is not Guid departmentId)
        {
            return;
        }

        await departmentAuthorization.EnsureDepartmentEmployeeAccessAsync(userId, departmentId, cancellationToken);
    }

    private async Task EnsureSessionRoleAsync(
        Guid userId,
        Guid defenceSessionId,
        AnnualRoleType roleType,
        CancellationToken cancellationToken)
    {
        bool hasRole = await annualRoleQueries.HasRoleForSessionAsync(
            userId,
            defenceSessionId,
            roleType,
            cancellationToken);

        if (!hasRole)
        {
            string message = roleType == AnnualRoleType.DepartmentHead
                ? AuthorizationMessages.NotDepartmentHead
                : AuthorizationMessages.MissingSessionRole;

            throw new DomainException(message);
        }
    }
}
