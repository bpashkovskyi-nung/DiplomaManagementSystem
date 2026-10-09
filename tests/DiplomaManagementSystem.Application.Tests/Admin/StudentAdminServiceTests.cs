using DiplomaManagementSystem.Application.Admin.Students;
using DiplomaManagementSystem.Application.Admin.Students.Dtos;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Identity.Contracts;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Persistence.Contracts;
using DiplomaManagementSystem.Application.Security;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;
using DiplomaManagementSystem.Infrastructure.Persistence;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiplomaManagementSystem.Application.Tests.Admin;

public sealed class StudentAdminServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ApplicationDbContext _dbContext;
    private readonly StudentAdminService _service;
    private readonly Guid _departmentId;

    public StudentAdminServiceTests()
    {
        string databaseName = Guid.NewGuid().ToString();
        ServiceCollection services = new();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddLogging();
        services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(identityOptions => { identityOptions.User.RequireUniqueEmail = true; })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        AdminDepartmentTestSupport.RegisterDepartmentServices(services);
        services.AddSingleton(new EmailDomainValidator(Microsoft.Extensions.Options.Options.Create(new SecurityOptions())));
        services.AddSingleton<DiplomaCreationService>();
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();
        services.AddScoped<StudentAdminService>();

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<ApplicationDbContext>();
        _departmentId = AdminDepartmentTestSupport.SeedDefaultDepartmentAsync(_dbContext).GetAwaiter().GetResult();
        _service = _serviceProvider.GetRequiredService<StudentAdminService>();
    }

    [Fact]
    public async Task CreateAsync_WhenValid_CreatesStudentInSession()
    {
        (Guid sessionId, Guid groupId) = await SeedSessionWithGroupAsync();
        await SeedRoleAsync(RoleNames.Student);

        Guid studentId = await _service.CreateAsync(
            new StudentFormDto(null, "Student One", "student@test.com", sessionId, groupId));

        ApplicationUser? student = await _dbContext.Users.FindAsync(studentId);
        Assert.NotNull(student);
        Assert.Equal(UserKind.Student, student.UserKind);
        Assert.Equal(sessionId, student.DefenceSessionId);
        Assert.Equal(groupId, student.StudyGroupId);
        Assert.True(await _dbContext.Diplomas.AnyAsync(diploma => diploma.StudentId == studentId));
    }

    [Fact]
    public async Task CreateAsync_WhenGroupNotInSession_Throws()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid otherSessionId = await SeedSessionAsync(DefenceSessionType.Master);
        Guid groupId = await SeedGroupAsync(otherSessionId, "КН-42");
        await SeedRoleAsync(RoleNames.Student);

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new StudentFormDto(null, "Student One", "student@test.com", sessionId, groupId)));
    }

    [Fact]
    public async Task DeleteAsync_WhenHasDiploma_Throws()
    {
        (Guid sessionId, Guid groupId) = await SeedSessionWithGroupAsync();
        Guid studentId = await SeedStudentAsync("Student One", "student@test.com", sessionId, groupId);
        _dbContext.Diplomas.Add(new Diploma
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            DefenceSessionId = sessionId,
            SupervisorAssignmentStatus = SupervisorAssignmentStatus.Rejected,
            ReviewAssignmentStatus = ReviewAssignmentStatus.NotAssigned,
            LifecycleStatus = DiplomaLifecycleStatus.AwaitingSupervisor,
            AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => _service.DeleteAsync(studentId));
    }

    private async Task<Guid> SeedSessionAsync(DefenceSessionType type = DefenceSessionType.Bachelor)
    {
        var sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = type,
            Status = DefenceSessionStatus.Active,
            DepartmentId = _departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return sessionId;
    }

    private async Task<Guid> SeedGroupAsync(Guid sessionId, string name)
    {
        var groupId = Guid.NewGuid();
        _dbContext.StudyGroups.Add(new StudyGroup
        {
            Id = groupId,
            Name = name,
            DefenceSessionId = sessionId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return groupId;
    }

    private async Task<(Guid SessionId, Guid GroupId)> SeedSessionWithGroupAsync()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid groupId = await SeedGroupAsync(sessionId, "КН-41");
        return (sessionId, groupId);
    }

    private async Task<Guid> SeedStudentAsync(string fullName, string email, Guid sessionId, Guid groupId)
    {
        var studentId = Guid.NewGuid();
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = studentId,
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FullName = fullName,
            UserKind = UserKind.Student,
            DefenceSessionId = sessionId,
            StudyGroupId = groupId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return studentId;
    }

    private async Task SeedRoleAsync(string roleName)
    {
        RoleManager<IdentityRole<Guid>> roleManager = _serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
            });
        }
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _dbContext.Dispose();
    }
}
