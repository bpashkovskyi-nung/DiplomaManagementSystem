using DiplomaManagementSystem.Application.Admin.AnnualRoles;
using DiplomaManagementSystem.Application.Admin.AnnualRoles.Contracts;
using DiplomaManagementSystem.Application.Admin.DefenceSessions;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Contracts;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Contracts;
using DiplomaManagementSystem.Application.Admin.Employees;
using DiplomaManagementSystem.Application.Admin.Employees.Contracts;
using DiplomaManagementSystem.Application.Admin.Students;
using DiplomaManagementSystem.Application.Admin.Students.Contracts;
using DiplomaManagementSystem.Application.Admin.StudyGroups;
using DiplomaManagementSystem.Application.Admin.StudyGroups.Contracts;
using DiplomaManagementSystem.Application.AdminPreview;
using DiplomaManagementSystem.Application.AdminPreview.Contracts;
using DiplomaManagementSystem.Application.Audit;
using DiplomaManagementSystem.Application.Audit.Contracts;
using DiplomaManagementSystem.Application.Authorization;
using DiplomaManagementSystem.Application.Authorization.Contracts;
using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Common.Contracts;
using DiplomaManagementSystem.Application.Documents;
using DiplomaManagementSystem.Application.Documents.Contracts;
using DiplomaManagementSystem.Application.Employee;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Validation;
using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins;
using DiplomaManagementSystem.Application.SuperAdmin.DepartmentAdmins.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Departments;
using DiplomaManagementSystem.Application.SuperAdmin.Departments.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport.Contracts;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Import;
using DiplomaManagementSystem.Application.Import.Contracts;
using DiplomaManagementSystem.Application.Import.Validation;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Secretary;
using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Documents;
using DiplomaManagementSystem.Application.Secretary.Documents.Contracts;
using DiplomaManagementSystem.Application.Secretary.Validation;
using DiplomaManagementSystem.Application.Security;
using DiplomaManagementSystem.Application.Student;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Validation;
using DiplomaManagementSystem.Domain.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<DefaultDepartmentResolver>();
        services.AddScoped<CurrentDepartmentResolver>();
        services.AddScoped<IDepartmentAuthorizationService, DepartmentAuthorizationService>();
        services.AddScoped<IFacultyAdminService, FacultyAdminService>();
        services.AddScoped<IDepartmentAdminService, DepartmentAdminService>();
        services.AddScoped<IDepartmentAdminAssignmentService, DepartmentAdminAssignmentService>();
        services.AddScoped<IOrganizationStructureImportService, OrganizationStructureImportService>();
        services.AddSingleton<DiplomaWorkflowInvariantValidator>();
        services.AddScoped<DefaultDepartmentResolver>();
        services.AddSingleton<DiplomaCreationService>();
        services.AddSingleton<DiplomaTopicService>();
        services.AddSingleton<AdmissionReadinessService>();
        services.AddSingleton<DiplomaLifecycleService>();
        services.AddSingleton<SupervisorSelectionService>();
        services.AddSingleton<SupervisorConfirmationService>();
        services.AddSingleton<TopicReviewService>();
        services.AddSingleton<AdmissionWorkflowService>();
        services.AddSingleton<ReviewerAssignmentService>();
        services.AddSingleton<DiplomaAdmissionService>();
        services.AddSingleton<SecretarySupervisorOverrideService>();
        services.AddSingleton<WorkReadinessService>();
        services.AddSingleton<DefenceSessionArchiveService>();
        services.AddSingleton<EmailDomainValidator>();

        services.AddValidatorsFromAssemblyContaining<StudentImportRowValidator>();
        services.AddValidatorsFromAssemblyContaining<StudyGroupFormValidator>();
        services.AddValidatorsFromAssemblyContaining<EmployeeFormValidator>();
        services.AddValidatorsFromAssemblyContaining<SetEmployeeWorkloadLimitValidator>();
        services.AddValidatorsFromAssemblyContaining<SelectSupervisorValidator>();
        services.AddValidatorsFromAssemblyContaining<SupervisorRejectValidator>();
        services.AddValidatorsFromAssemblyContaining<CompleteCheckpointValidator>();
        services.AddValidatorsFromAssemblyContaining<AssignReviewerValidator>();
        services.AddValidatorsFromAssemblyContaining<TopicOrderGenerateRequestValidator>();

        services.AddOptions<BootstrapOptions>()
            .BindConfiguration(BootstrapOptions.SectionName);
        services.AddOptions<SecurityOptions>()
            .BindConfiguration(SecurityOptions.SectionName);
        services.AddOptions<ImportOptions>()
            .BindConfiguration(ImportOptions.SectionName);
        services.AddOptions<SecretaryOptions>()
            .BindConfiguration(SecretaryOptions.SectionName);
        services.AddOptions<AppLocalizationOptions>()
            .BindConfiguration(AppLocalizationOptions.SectionName);
        services.AddOptions<FileStorageOptions>()
            .BindConfiguration(FileStorageOptions.SectionName);
        services.AddOptions<DepartmentOptions>()
            .BindConfiguration(DepartmentOptions.SectionName);
        services.AddOptions<OrganizationOptions>()
            .BindConfiguration(OrganizationOptions.SectionName);

        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<IImportFileParser, ImportFileParser>();
        services.AddScoped<IArchiveGuard, ArchiveGuard>();
        services.AddScoped<IAuditLogWriter, AuditLogWriter>();
        services.AddScoped<IDiplomaAuthorizationService, DiplomaAuthorizationService>();
        services.AddScoped<ImportRowProcessor>();
        services.AddScoped<IStudentImportService, StudentImportService>();
        services.AddScoped<IEmployeeImportService, EmployeeImportService>();
        services.AddScoped<IDefenceSessionService, DefenceSessionService>();
        services.AddScoped<IStudyGroupAdminService, StudyGroupAdminService>();
        services.AddScoped<IStudentAdminService, StudentAdminService>();
        services.AddScoped<IEmployeeAdminService, EmployeeAdminService>();
        services.AddScoped<IAnnualRoleService, AnnualRoleService>();
        services.AddScoped<IEmployeeWorkloadLimitAdminService, EmployeeWorkloadLimitAdminService>();
        services.AddScoped<IEmployeeWorkloadLimitService, EmployeeWorkloadLimitService>();
        services.AddScoped<ISecretaryAccessService, SecretaryAccessService>();
        services.AddScoped<ISecretaryDashboardService, SecretaryDashboardService>();
        services.AddScoped<ISecretaryDiplomaListService, SecretaryDiplomaListService>();
        services.AddScoped<IStudentDiplomaService, StudentDiplomaService>();
        services.AddScoped<IEmployeeHomeService, EmployeeHomeService>();
        services.AddScoped<ISupervisorWorkflowService, SupervisorWorkflowService>();
        services.AddScoped<ISupervisorDiplomaListService, SupervisorDiplomaListService>();
        services.AddScoped<IReviewerDiplomaListService, ReviewerDiplomaListService>();
        services.AddScoped<ISupervisorDiplomaDetailsService, SupervisorDiplomaDetailsService>();
        services.AddScoped<IDepartmentHeadWorkflowService, DepartmentHeadWorkflowService>();
        services.AddScoped<IAdmissionReviewService, AdmissionReviewService>();
        services.AddScoped<IDiplomaDocumentService, DiplomaDocumentService>();
        services.AddScoped<DiplomaDetailsAssembler>();
        services.AddScoped<ISecretaryDiplomaDetailsService, SecretaryDiplomaDetailsService>();
        services.AddScoped<ISecretaryDiplomaActionService, SecretaryDiplomaActionService>();
        services.AddScoped<IAdmittedReportService, AdmittedReportService>();
        services.AddScoped<TopicOrderDocxGenerator>();
        services.AddScoped<ITopicOrderDocumentService, TopicOrderDocumentService>();
        services.AddScoped<IAdminPreviewUserLookup, AdminPreviewUserLookup>();
        services.AddScoped<IAdminPreviewUserPickerService, AdminPreviewUserPickerService>();

        return services;
    }
}
