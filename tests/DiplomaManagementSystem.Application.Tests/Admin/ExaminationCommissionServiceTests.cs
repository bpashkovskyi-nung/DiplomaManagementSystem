using DiplomaManagementSystem.Application.Admin.ExaminationCommission;
using DiplomaManagementSystem.Application.Admin.ExaminationCommission.Dtos;
using DiplomaManagementSystem.Application.Departments;
using DiplomaManagementSystem.Application.Departments.Contracts;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Tests.Admin;

public sealed class ExaminationCommissionServiceTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TestDepartmentContext _departmentContext;
    private readonly ExaminationCommissionService _service;
    private readonly Guid _departmentId;

    public ExaminationCommissionServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _departmentContext = new TestDepartmentContext();
        _departmentId = OrganizationTestData.SeedDepartmentAsync(_dbContext).GetAwaiter().GetResult();
        _departmentContext.CurrentDepartmentId = _departmentId;

        TestDepartmentAuthorizationService departmentAuthorization = new(_dbContext);
        CurrentDepartmentResolver resolver = new(_departmentContext, departmentAuthorization, _dbContext);
        _service = new ExaminationCommissionService(_dbContext, resolver, departmentAuthorization);
    }

    [Fact]
    public async Task GetEditorAsync_WhenSessionMissing_ReturnsNull()
    {
        ExaminationCommissionEditorDto? editor = await _service.GetEditorAsync(Guid.NewGuid());

        Assert.Null(editor);
    }

    [Fact]
    public async Task SaveAsync_InternalChairAndThreeMembers_PersistsSnapshots()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid chairId = await SeedEmployeeAsync("Голова Внутрішній", EmployeeAcademicRank.Professor);
        Guid member1 = await SeedEmployeeAsync("Член Один", EmployeeAcademicRank.AssociateProfessor, "m1@test.local");
        Guid member2 = await SeedEmployeeAsync("Член Два", EmployeeAcademicRank.SeniorLecturer, "m2@test.local");
        Guid member3 = await SeedEmployeeAsync("Член Три", EmployeeAcademicRank.Lecturer, "m3@test.local");

        await _service.SaveAsync(new SaveExaminationCommissionDto(
            sessionId,
            new SaveExaminationCommissionParticipantDto(false, chairId, null, null),
            [
                new SaveExaminationCommissionParticipantDto(false, member1, null, null),
                new SaveExaminationCommissionParticipantDto(false, member2, null, null),
                new SaveExaminationCommissionParticipantDto(false, member3, null, null),
            ]));

        ExaminationCommissionEditorDto? editor = await _service.GetEditorAsync(sessionId);
        Assert.NotNull(editor);
        Assert.NotNull(editor.Commission.Chair);
        Assert.Equal(chairId, editor.Commission.Chair.EmployeeId);
        Assert.Equal("Голова Внутрішній", editor.Commission.Chair.FullName);
        Assert.Equal("Професор", editor.Commission.Chair.Position);
        Assert.Equal(3, editor.Commission.Members.Count);
        Assert.All(editor.Commission.Members, member => Assert.False(string.IsNullOrWhiteSpace(member.Position)));
    }

    [Fact]
    public async Task SaveAsync_WithExternalParticipants_PersistsManualFields()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid member1 = await SeedEmployeeAsync("Член Один", EmployeeAcademicRank.AssociateProfessor, "m1@test.local");
        Guid member2 = await SeedEmployeeAsync("Член Два", EmployeeAcademicRank.SeniorLecturer, "m2@test.local");

        await _service.SaveAsync(new SaveExaminationCommissionDto(
            sessionId,
            new SaveExaminationCommissionParticipantDto(true, null, "Зовнішній Голова", "д.т.н., професор НУ"),
            [
                new SaveExaminationCommissionParticipantDto(false, member1, null, null),
                new SaveExaminationCommissionParticipantDto(false, member2, null, null),
                new SaveExaminationCommissionParticipantDto(true, null, "Зовнішній Член", "к.т.н."),
            ]));

        ExaminationCommissionEditorDto? editor = await _service.GetEditorAsync(sessionId);
        Assert.NotNull(editor);
        Assert.Null(editor.Commission.Chair!.EmployeeId);
        Assert.Equal("Зовнішній Голова", editor.Commission.Chair.FullName);
        Assert.Equal("д.т.н., професор НУ", editor.Commission.Chair.Position);
        Assert.Contains(editor.Commission.Members, member => member.FullName == "Зовнішній Член" && member.EmployeeId is null);
    }

    [Fact]
    public async Task SaveAsync_WhenFewerThanThreeMembers_Throws()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid chairId = await SeedEmployeeAsync("Голова", EmployeeAcademicRank.Professor);
        Guid member1 = await SeedEmployeeAsync("Член Один", EmployeeAcademicRank.AssociateProfessor, "m1@test.local");
        Guid member2 = await SeedEmployeeAsync("Член Два", EmployeeAcademicRank.SeniorLecturer, "m2@test.local");

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            _service.SaveAsync(new SaveExaminationCommissionDto(
                sessionId,
                new SaveExaminationCommissionParticipantDto(false, chairId, null, null),
                [
                    new SaveExaminationCommissionParticipantDto(false, member1, null, null),
                    new SaveExaminationCommissionParticipantDto(false, member2, null, null),
                ])));

        Assert.Contains("щонайменше 3", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_WhenInternalWithoutRank_Throws()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid chairId = await SeedEmployeeAsync("Голова без звання", academicRank: null);
        Guid member1 = await SeedEmployeeAsync("Член Один", EmployeeAcademicRank.AssociateProfessor, "m1@test.local");
        Guid member2 = await SeedEmployeeAsync("Член Два", EmployeeAcademicRank.SeniorLecturer, "m2@test.local");
        Guid member3 = await SeedEmployeeAsync("Член Три", EmployeeAcademicRank.Lecturer, "m3@test.local");

        DomainException exception = await Assert.ThrowsAsync<DomainException>(() =>
            _service.SaveAsync(new SaveExaminationCommissionDto(
                sessionId,
                new SaveExaminationCommissionParticipantDto(false, chairId, null, null),
                [
                    new SaveExaminationCommissionParticipantDto(false, member1, null, null),
                    new SaveExaminationCommissionParticipantDto(false, member2, null, null),
                    new SaveExaminationCommissionParticipantDto(false, member3, null, null),
                ])));

        Assert.Contains("вчене звання", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveAsync_WhenEmployeeFromOtherDepartment_Throws()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid otherDepartmentId = await OrganizationTestData.SeedDepartmentAsync(_dbContext, "Ф2", "К2");
        Guid foreignId = await SeedEmployeeAsync(
            "Чужий",
            EmployeeAcademicRank.Professor,
            "foreign@test.local",
            otherDepartmentId);
        Guid member1 = await SeedEmployeeAsync("Член Один", EmployeeAcademicRank.AssociateProfessor, "m1@test.local");
        Guid member2 = await SeedEmployeeAsync("Член Два", EmployeeAcademicRank.SeniorLecturer, "m2@test.local");
        Guid member3 = await SeedEmployeeAsync("Член Три", EmployeeAcademicRank.Lecturer, "m3@test.local");

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.SaveAsync(new SaveExaminationCommissionDto(
                sessionId,
                new SaveExaminationCommissionParticipantDto(false, foreignId, null, null),
                [
                    new SaveExaminationCommissionParticipantDto(false, member1, null, null),
                    new SaveExaminationCommissionParticipantDto(false, member2, null, null),
                    new SaveExaminationCommissionParticipantDto(false, member3, null, null),
                ])));
    }

    [Fact]
    public async Task SaveAsync_SnapshotStableAfterEmployeeRename()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid chairId = await SeedEmployeeAsync("Старе Імʼя", EmployeeAcademicRank.Professor);
        Guid member1 = await SeedEmployeeAsync("Член Один", EmployeeAcademicRank.AssociateProfessor, "m1@test.local");
        Guid member2 = await SeedEmployeeAsync("Член Два", EmployeeAcademicRank.SeniorLecturer, "m2@test.local");
        Guid member3 = await SeedEmployeeAsync("Член Три", EmployeeAcademicRank.Lecturer, "m3@test.local");

        await _service.SaveAsync(new SaveExaminationCommissionDto(
            sessionId,
            new SaveExaminationCommissionParticipantDto(false, chairId, null, null),
            [
                new SaveExaminationCommissionParticipantDto(false, member1, null, null),
                new SaveExaminationCommissionParticipantDto(false, member2, null, null),
                new SaveExaminationCommissionParticipantDto(false, member3, null, null),
            ]));

        ApplicationUser user = await _dbContext.Users.SingleAsync(item => item.Id == chairId);
        user.FullName = "Нове Імʼя";
        DepartmentEmployee employee = await _dbContext.DepartmentEmployees.SingleAsync(item => item.UserId == chairId);
        employee.FullName = "Нове Імʼя";
        employee.AcademicRank = EmployeeAcademicRank.Assistant;
        await _dbContext.SaveChangesAsync();

        ExaminationCommissionEditorDto? editor = await _service.GetEditorAsync(sessionId);
        Assert.NotNull(editor);
        Assert.Equal("Старе Імʼя", editor.Commission.Chair!.FullName);
        Assert.Equal("Професор", editor.Commission.Chair.Position);
    }

    [Fact]
    public async Task SaveAsync_ReplacesExistingRoster()
    {
        Guid sessionId = await SeedSessionAsync();
        Guid chairId = await SeedEmployeeAsync("Голова", EmployeeAcademicRank.Professor);
        Guid member1 = await SeedEmployeeAsync("Член Один", EmployeeAcademicRank.AssociateProfessor, "m1@test.local");
        Guid member2 = await SeedEmployeeAsync("Член Два", EmployeeAcademicRank.SeniorLecturer, "m2@test.local");
        Guid member3 = await SeedEmployeeAsync("Член Три", EmployeeAcademicRank.Lecturer, "m3@test.local");
        Guid member4 = await SeedEmployeeAsync("Член Чотири", EmployeeAcademicRank.Assistant, "m4@test.local");

        await _service.SaveAsync(new SaveExaminationCommissionDto(
            sessionId,
            new SaveExaminationCommissionParticipantDto(false, chairId, null, null),
            [
                new SaveExaminationCommissionParticipantDto(false, member1, null, null),
                new SaveExaminationCommissionParticipantDto(false, member2, null, null),
                new SaveExaminationCommissionParticipantDto(false, member3, null, null),
            ]));

        await _service.SaveAsync(new SaveExaminationCommissionDto(
            sessionId,
            new SaveExaminationCommissionParticipantDto(true, null, "Новий Голова", "проф."),
            [
                new SaveExaminationCommissionParticipantDto(false, member2, null, null),
                new SaveExaminationCommissionParticipantDto(false, member3, null, null),
                new SaveExaminationCommissionParticipantDto(false, member4, null, null),
            ]));

        int count = await _dbContext.ExaminationCommissionParticipants.CountAsync(
            participant => participant.DefenceSessionId == sessionId);
        Assert.Equal(4, count);

        ExaminationCommissionEditorDto? editor = await _service.GetEditorAsync(sessionId);
        Assert.NotNull(editor);
        Assert.Equal("Новий Голова", editor.Commission.Chair!.FullName);
        Assert.DoesNotContain(editor.Commission.Members, member => member.EmployeeId == member1);
    }

    private async Task<Guid> SeedSessionAsync()
    {
        var sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = DefenceSessionType.Bachelor,
            Semester = 2,
            Status = DefenceSessionStatus.Active,
            DepartmentId = _departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return sessionId;
    }

    private async Task<Guid> SeedEmployeeAsync(
        string fullName,
        EmployeeAcademicRank? academicRank,
        string? email = null,
        Guid? departmentId = null)
    {
        var employeeId = Guid.NewGuid();
        string resolvedEmail = email ?? $"{employeeId:N}@test.local";
        Guid targetDepartmentId = departmentId ?? _departmentId;
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = employeeId,
            Email = resolvedEmail,
            UserName = resolvedEmail,
            FullName = fullName,
            UserKind = UserKind.Employee,
            AcademicRank = academicRank,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        _dbContext.DepartmentEmployees.Add(new DepartmentEmployee
        {
            Id = Guid.NewGuid(),
            DepartmentId = targetDepartmentId,
            UserId = employeeId,
            FullName = fullName,
            AcademicRank = academicRank,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return employeeId;
    }

    public void Dispose() => _dbContext.Dispose();

    private sealed class TestDepartmentAuthorizationService(ApplicationDbContext dbContext)
        : IDepartmentAuthorizationService
    {
        public Task<bool> IsSuperAdminAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IReadOnlyList<Guid>> GetAdminDepartmentIdsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> GetEmployeeDepartmentIdsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task EnsureDepartmentAdminAccessAsync(
            Guid userId,
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task EnsureDepartmentEmployeeAccessAsync(
            Guid userId,
            Guid departmentId,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public async Task EnsureSessionInDepartmentAsync(
            Guid sessionId,
            Guid departmentId,
            CancellationToken cancellationToken = default)
        {
            bool belongs = await dbContext.DefenceSessions
                .AsNoTracking()
                .AnyAsync(
                    session => session.Id == sessionId && session.DepartmentId == departmentId,
                    cancellationToken);

            if (!belongs)
            {
                throw new DomainException(DepartmentMessages.SessionNotInDepartment);
            }
        }
    }
}
