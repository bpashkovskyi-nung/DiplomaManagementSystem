using DiplomaManagementSystem.Application.Admin.StudyGroups;
using DiplomaManagementSystem.Application.Admin.StudyGroups.Dtos;
using DiplomaManagementSystem.Application.Identity;
using DiplomaManagementSystem.Application.Tests.Departments;
using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DiplomaManagementSystem.Application.Tests.Admin;

public sealed class StudyGroupAdminServiceTests : IDisposable
{
    private const string DefaultStudyForm = "очної форми навчання";

    private readonly ApplicationDbContext _dbContext;
    private readonly StudyGroupAdminService _service;

    public StudyGroupAdminServiceTests()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new ApplicationDbContext(options);
        _service = new StudyGroupAdminService(_dbContext);
    }

    [Fact]
    public async Task CreateAsync_WhenNameUniqueInSession_CreatesGroup()
    {
        (Guid sessionId, Guid specialtyId) = await SeedSessionAsync();
        Guid id = await _service.CreateAsync(new StudyGroupFormDto(null, sessionId, "КН-41", specialtyId, DefaultStudyForm));

        StudyGroup? group = await _dbContext.StudyGroups.FindAsync(id);
        Assert.NotNull(group);
        Assert.Equal("КН-41", group.Name);
        Assert.Equal(sessionId, group.DefenceSessionId);
        Assert.Equal(specialtyId, group.SpecialtyId);
        Assert.Equal(DefaultStudyForm, group.StudyForm);
    }

    [Fact]
    public async Task CreateAsync_WhenNameDuplicateInSameSession_Throws()
    {
        (Guid sessionId, Guid specialtyId) = await SeedSessionAsync();
        await _service.CreateAsync(new StudyGroupFormDto(null, sessionId, "КН-41", specialtyId, DefaultStudyForm));

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new StudyGroupFormDto(null, sessionId, "КН-41", specialtyId, DefaultStudyForm)));
    }

    [Fact]
    public async Task CreateAsync_WhenSpecialtyFromOtherDepartment_Throws()
    {
        (Guid sessionId, Guid specialtyId) = await SeedSessionAsync();
        (_, Guid otherSpecialtyId) = await OrganizationTestData.SeedDepartmentWithSpecialtyAsync(
            _dbContext,
            departmentName: "Інша кафедра",
            specialtyCode: "999");

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new StudyGroupFormDto(null, sessionId, "КН-41", otherSpecialtyId, DefaultStudyForm)));
    }

    [Fact]
    public async Task CreateAsync_WhenSameNameInDifferentSessions_Allowed()
    {
        (Guid sessionA, Guid specialtyA) = await SeedSessionAsync();
        (Guid sessionB, Guid specialtyB) = await SeedSessionAsync(DefenceSessionType.Master);

        await _service.CreateAsync(new StudyGroupFormDto(null, sessionA, "КН-41", specialtyA, DefaultStudyForm));
        Guid idB = await _service.CreateAsync(new StudyGroupFormDto(null, sessionB, "КН-41", specialtyB, DefaultStudyForm));

        StudyGroup? groupB = await _dbContext.StudyGroups.FindAsync(idB);
        Assert.NotNull(groupB);
        Assert.Equal(sessionB, groupB.DefenceSessionId);
    }

    [Fact]
    public async Task DeleteAsync_WhenEmpty_RemovesGroup()
    {
        (Guid sessionId, Guid specialtyId) = await SeedSessionAsync();
        Guid id = await _service.CreateAsync(new StudyGroupFormDto(null, sessionId, "КН-41", specialtyId, DefaultStudyForm));

        await _service.DeleteAsync(id);

        Assert.Null(await _dbContext.StudyGroups.FindAsync(id));
    }

    [Fact]
    public async Task DeleteAsync_WhenHasStudents_Throws()
    {
        (Guid sessionId, Guid specialtyId) = await SeedSessionAsync();
        Guid groupId = await _service.CreateAsync(new StudyGroupFormDto(null, sessionId, "КН-41", specialtyId, DefaultStudyForm));
        _dbContext.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "student@test.com",
            UserName = "student@test.com",
            FullName = "Student",
            UserKind = UserKind.Student,
            DefenceSessionId = sessionId,
            StudyGroupId = groupId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() => _service.DeleteAsync(groupId));
    }

    [Fact]
    public async Task GetAllAsync_WhenFilteredBySession_ReturnsOnlySessionGroups()
    {
        (Guid sessionA, Guid specialtyA) = await SeedSessionAsync();
        (Guid sessionB, Guid specialtyB) = await SeedSessionAsync(DefenceSessionType.Master);
        await _service.CreateAsync(new StudyGroupFormDto(null, sessionA, "КН-41", specialtyA, DefaultStudyForm));
        await _service.CreateAsync(new StudyGroupFormDto(null, sessionB, "КН-42", specialtyB, DefaultStudyForm));

        IReadOnlyList<StudyGroupListItemDto> items = await _service.GetAllAsync(sessionA);

        StudyGroupListItemDto item = Assert.Single(items);
        Assert.Equal("КН-41", item.Name);
        Assert.Equal("123 — Тестова спеціальність", item.SpecialtyLabel);
    }

    [Fact]
    public async Task UpdateAsync_WhenSessionChanges_Throws()
    {
        (Guid sessionId, Guid specialtyId) = await SeedSessionAsync();
        Guid groupId = await _service.CreateAsync(new StudyGroupFormDto(null, sessionId, "КН-41", specialtyId, DefaultStudyForm));

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.UpdateAsync(
                groupId,
                new StudyGroupFormDto(groupId, Guid.NewGuid(), "КН-41", specialtyId, DefaultStudyForm)));
    }

    [Fact]
    public async Task GetForEditAsync_WhenExists_ReturnsForm()
    {
        (Guid sessionId, Guid specialtyId) = await SeedSessionAsync();
        Guid groupId = await _service.CreateAsync(new StudyGroupFormDto(null, sessionId, "КН-41", specialtyId, DefaultStudyForm));

        StudyGroupFormDto? form = await _service.GetForEditAsync(groupId);

        Assert.NotNull(form);
        Assert.Equal(groupId, form.Id);
        Assert.Equal("КН-41", form.Name);
    }

    [Fact]
    public async Task CreateAsync_WhenSessionArchived_Throws()
    {
        (Guid sessionId, Guid specialtyId) = await SeedSessionAsync();
        DefenceSession session = (await _dbContext.DefenceSessions.FindAsync(sessionId))!;
        session.Status = DefenceSessionStatus.Archived;
        await _dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<DomainException>(() =>
            _service.CreateAsync(new StudyGroupFormDto(null, sessionId, "КН-41", specialtyId, DefaultStudyForm)));
    }

    private async Task<(Guid SessionId, Guid SpecialtyId)> SeedSessionAsync(
        DefenceSessionType type = DefenceSessionType.Bachelor)
    {
        (Guid departmentId, Guid specialtyId) = await OrganizationTestData.SeedDepartmentWithSpecialtyAsync(_dbContext);

        Guid sessionId = Guid.NewGuid();
        _dbContext.DefenceSessions.Add(new DefenceSession
        {
            Id = sessionId,
            Year = 2026,
            Type = type,
            Status = DefenceSessionStatus.Active,
            DepartmentId = departmentId,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await _dbContext.SaveChangesAsync();
        return (sessionId, specialtyId);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
