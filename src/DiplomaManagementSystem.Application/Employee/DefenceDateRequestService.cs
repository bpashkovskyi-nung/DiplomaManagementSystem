using DiplomaManagementSystem.Application.Authorization;
using DiplomaManagementSystem.Application.Authorization.Contracts;
using DiplomaManagementSystem.Application.Common.Contracts;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Employee;

internal sealed class DefenceDateRequestService(
    IApplicationDbContext dbContext,
    IArchiveGuard archiveGuard,
    IDiplomaAuthorizationService diplomaAuthorizationService,
    IUserDisplayQueries userDisplayQueries,
    DefenceDatePreferenceService preferenceService) : IDefenceDateRequestService
{
    private const string MissingLabel = "—";

    public async Task<DefenceDateRequestFormDto?> GetFormForStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        Diploma? diploma = await dbContext.Diplomas
            .AsNoTracking()
            .Include(item => item.DefenceSession)
            .Include(item => item.DefenceDatePreference!)
            .ThenInclude(preference => preference.DefenceDateOption)
            .Where(item => item.StudentId == studentId)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (diploma is null)
        {
            return null;
        }

        return await BuildFormAsync(diploma, cancellationToken);
    }

    public async Task<DefenceDateRequestFormDto?> GetFormForSupervisorAsync(
        Guid supervisorId,
        Guid diplomaId,
        CancellationToken cancellationToken = default)
    {
        Diploma? diploma = await dbContext.Diplomas
            .AsNoTracking()
            .Include(item => item.DefenceSession)
            .Include(item => item.DefenceDatePreference!)
            .ThenInclude(preference => preference.DefenceDateOption)
            .FirstOrDefaultAsync(
                item => item.Id == diplomaId
                        && item.SupervisorId == supervisorId
                        && item.SupervisorAssignmentStatus == SupervisorAssignmentStatus.Confirmed,
                cancellationToken);

        if (diploma is null)
        {
            return null;
        }

        return await BuildFormAsync(diploma, cancellationToken);
    }

    public async Task RequestAsStudentAsync(
        Guid studentId,
        RequestDefenceDateDto request,
        CancellationToken cancellationToken = default)
    {
        Diploma diploma = await GetWritableDiplomaAsync(request.DiplomaId, cancellationToken);
        if (diploma.StudentId != studentId)
        {
            throw new DomainException(AuthorizationMessages.DiplomaNotFound);
        }

        await CreatePreferenceAsync(
            studentId,
            diploma,
            request.DefenceDateOptionId,
            DefenceDateRequesterType.Student,
            cancellationToken);
    }

    public async Task RequestAsSupervisorAsync(
        Guid supervisorId,
        RequestDefenceDateDto request,
        CancellationToken cancellationToken = default)
    {
        await diplomaAuthorizationService.EnsureCanPerformAsync(
            supervisorId,
            request.DiplomaId,
            DiplomaAction.RequestDefenceDate,
            cancellationToken);

        Diploma diploma = await GetWritableDiplomaAsync(request.DiplomaId, cancellationToken);
        await CreatePreferenceAsync(
            supervisorId,
            diploma,
            request.DefenceDateOptionId,
            DefenceDateRequesterType.Supervisor,
            cancellationToken);
    }

    private async Task CreatePreferenceAsync(
        Guid requesterUserId,
        Diploma diploma,
        Guid optionId,
        DefenceDateRequesterType requesterType,
        CancellationToken cancellationToken)
    {
        archiveGuard.EnsureWritable(diploma.DefenceSession);

        DefenceDateOption? option = await dbContext.DefenceDateOptions
            .FirstOrDefaultAsync(
                item => item.Id == optionId && item.DefenceSessionId == diploma.DefenceSessionId,
                cancellationToken);

        if (option is null)
        {
            throw new DomainException("Selected defence date is not available for this session.");
        }

        bool exists = await dbContext.DefenceDatePreferences
            .AnyAsync(item => item.DiplomaId == diploma.Id, cancellationToken);

        preferenceService.EnsureCanRequest(diploma, diploma.DefenceSession, option, exists);

        dbContext.DefenceDatePreferences.Add(new DefenceDatePreference
        {
            Id = Guid.NewGuid(),
            DiplomaId = diploma.Id,
            DefenceDateOptionId = option.Id,
            RequesterType = requesterType,
            RequesterUserId = requesterUserId,
            RequestedAt = DateTimeOffset.UtcNow,
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new DomainException("A defence date preference already exists for this diploma.");
        }
    }

    private async Task<DefenceDateRequestFormDto> BuildFormAsync(
        Diploma diploma,
        CancellationToken cancellationToken)
    {
        List<DefenceDateOption> options = await dbContext.DefenceDateOptions
            .AsNoTracking()
            .Where(item => item.DefenceSessionId == diploma.DefenceSessionId)
            .OrderBy(item => item.Date)
            .ToListAsync(cancellationToken);

        Dictionary<Guid, ApplicationUser> students = await userDisplayQueries.LoadUsersAsync(
            [diploma.StudentId],
            cancellationToken);
        students.TryGetValue(diploma.StudentId, out ApplicationUser? student);

        bool canRequest = diploma.AdmissionStatus == DiplomaAdmissionStatus.Admitted
                          && diploma.DefenceDatePreference is null
                          && diploma.DefenceSession.Status == DefenceSessionStatus.Active
                          && options.Count > 0;

        return new DefenceDateRequestFormDto(
            diploma.Id,
            student?.FullName ?? MissingLabel,
            canRequest,
            diploma.DefenceDatePreference?.DefenceDateOption.Date,
            diploma.DefenceDate,
            options.Select(item => new DefenceDateChoiceDto(item.Id, item.Date)).ToList());
    }

    private async Task<Diploma> GetWritableDiplomaAsync(Guid diplomaId, CancellationToken cancellationToken)
    {
        Diploma? diploma = await dbContext.Diplomas
            .Include(item => item.DefenceSession)
            .FirstOrDefaultAsync(item => item.Id == diplomaId, cancellationToken);

        if (diploma is null)
        {
            throw new DomainException(AuthorizationMessages.DiplomaNotFound);
        }

        return diploma;
    }
}
