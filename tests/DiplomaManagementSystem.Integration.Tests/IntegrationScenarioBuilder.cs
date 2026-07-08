using DiplomaManagementSystem.Application.Admin.AnnualRoles.Contracts;
using DiplomaManagementSystem.Application.Admin.AnnualRoles.Dtos;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Application.Admin.StudyGroups.Contracts;
using DiplomaManagementSystem.Application.Admin.StudyGroups.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Services;
using DiplomaManagementSystem.Integration.Tests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Integration.Tests;

internal sealed class IntegrationScenarioBuilder
{
    private readonly IServiceProvider _serviceProvider;

    public IntegrationScenarioBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Guid> SeedSessionOnlyAsync()
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(scope.ServiceProvider);
        IDefenceSessionService defenceSessionService = scope.ServiceProvider.GetRequiredService<IDefenceSessionService>();
        return await defenceSessionService.CreateAsync(
            new DefenceSessionFormDto(null, 2026, DefenceSessionType.Bachelor, 1));
    }

    public async Task<IntegrationScenario> SeedFullScenarioAsync()
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(services);

        string suffix = Guid.NewGuid().ToString("N")[..8];

        IDefenceSessionService defenceSessionService = services.GetRequiredService<IDefenceSessionService>();
        IStudyGroupAdminService studyGroupAdminService = services.GetRequiredService<IStudyGroupAdminService>();
        IUserProvisioningService userProvisioningService = services.GetRequiredService<IUserProvisioningService>();
        IAnnualRoleService annualRoleService = services.GetRequiredService<IAnnualRoleService>();
        IApplicationDbContext dbContext = services.GetRequiredService<IApplicationDbContext>();
        IDiplomaQueries diplomaQueries = services.GetRequiredService<IDiplomaQueries>();

        Guid sessionId = await defenceSessionService.CreateAsync(
            new DefenceSessionFormDto(null, 2026, DefenceSessionType.Bachelor, 1));

        Guid groupId = await studyGroupAdminService.CreateAsync(
            new StudyGroupFormDto(null, sessionId, $"КН-41-{suffix}"));

        ApplicationUser student = await userProvisioningService.CreateStudentAsync(
            "Student One",
            $"student.{suffix}@test.local",
            sessionId,
            groupId);
        ApplicationUser supervisor = await userProvisioningService.CreateEmployeeAsync(
            "Supervisor One",
            $"supervisor.{suffix}@test.local");
        ApplicationUser head = await userProvisioningService.CreateEmployeeAsync(
            "Head One",
            $"head.{suffix}@test.local");
        ApplicationUser secretary = await userProvisioningService.CreateEmployeeAsync(
            "Secretary One",
            $"secretary.{suffix}@test.local");
        ApplicationUser reviewer = await userProvisioningService.CreateEmployeeAsync(
            "Reviewer One",
            $"reviewer.{suffix}@test.local");
        ApplicationUser antiPlagiarism = await userProvisioningService.CreateEmployeeAsync(
            "Anti Plagiarism",
            $"antiplag.{suffix}@test.local");
        ApplicationUser formatting = await userProvisioningService.CreateEmployeeAsync(
            "Formatting Reviewer",
            $"formatting.{suffix}@test.local");

        await IntegrationDepartmentHelper.AssignEmployeeAsync(services, supervisor.Id, supervisor.FullName);
        await IntegrationDepartmentHelper.AssignEmployeeAsync(services, head.Id, head.FullName);
        await IntegrationDepartmentHelper.AssignEmployeeAsync(services, secretary.Id, secretary.FullName);
        await IntegrationDepartmentHelper.AssignEmployeeAsync(services, reviewer.Id, reviewer.FullName);
        await IntegrationDepartmentHelper.AssignEmployeeAsync(services, antiPlagiarism.Id, antiPlagiarism.FullName);
        await IntegrationDepartmentHelper.AssignEmployeeAsync(services, formatting.Id, formatting.FullName);

        await annualRoleService.AssignAsync(new AssignAnnualRoleDto(sessionId, AnnualRoleType.DepartmentHead, head.Id));
        await annualRoleService.AssignAsync(
            new AssignAnnualRoleDto(sessionId, AnnualRoleType.ExamCommissionSecretary, secretary.Id));
        await annualRoleService.AssignAsync(
            new AssignAnnualRoleDto(sessionId, AnnualRoleType.AntiPlagiarismOfficer, antiPlagiarism.Id));
        await annualRoleService.AssignAsync(
            new AssignAnnualRoleDto(sessionId, AnnualRoleType.FormattingReviewer, formatting.Id));

        DiplomaCreationService diplomaCreationService = new();
        IReadOnlyList<Diploma> diplomas = diplomaCreationService.CreateForStudents(
            [new DiplomaStudentCandidate(student.Id, UserKind.Student)],
            sessionId,
            new HashSet<Guid>());
        dbContext.Diplomas.AddRange(diplomas);
        await dbContext.SaveChangesAsync();

        List<Diploma> sessionDiplomas = await diplomaQueries.ListForSessionReadAsync(sessionId);
        Guid diplomaId = sessionDiplomas.Single(diploma => diploma.StudentId == student.Id).Id;

        return new IntegrationScenario(
            sessionId,
            groupId,
            diplomaId,
            student.Id,
            supervisor.Id,
            head.Id,
            secretary.Id,
            reviewer.Id,
            antiPlagiarism.Id,
            formatting.Id,
            $"КН-41-{suffix}");
    }

    public async Task<StudentOnlyScenario> SeedStudentWithoutDiplomaAsync()
    {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        IServiceProvider services = scope.ServiceProvider;
        await IntegrationDepartmentHelper.EnsureDefaultDepartmentContextAsync(services);

        string suffix = Guid.NewGuid().ToString("N")[..8];

        IDefenceSessionService defenceSessionService = services.GetRequiredService<IDefenceSessionService>();
        IStudyGroupAdminService studyGroupAdminService = services.GetRequiredService<IStudyGroupAdminService>();
        IUserProvisioningService userProvisioningService = services.GetRequiredService<IUserProvisioningService>();

        Guid sessionId = await defenceSessionService.CreateAsync(
            new DefenceSessionFormDto(null, 2026, DefenceSessionType.Bachelor, 1));

        Guid groupId = await studyGroupAdminService.CreateAsync(
            new StudyGroupFormDto(null, sessionId, $"КН-42-{suffix}"));

        ApplicationUser student = await userProvisioningService.CreateStudentAsync(
            "Student Without Diploma",
            $"student.nodiploma.{suffix}@test.local",
            sessionId,
            groupId);

        return new StudentOnlyScenario(sessionId, groupId, student.Id, $"КН-42-{suffix}");
    }
}

internal sealed record StudentOnlyScenario(
    Guid SessionId,
    Guid GroupId,
    Guid StudentId,
    string StudyGroupName);

internal sealed record IntegrationScenario(
    Guid SessionId,
    Guid GroupId,
    Guid DiplomaId,
    Guid StudentId,
    Guid SupervisorId,
    Guid HeadId,
    Guid SecretaryId,
    Guid ReviewerId,
    Guid AntiPlagiarismId,
    Guid FormattingId,
    string StudyGroupName);
