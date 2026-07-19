using DiplomaManagementSystem.Domain.Entities;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Domain.Services;

namespace DiplomaManagementSystem.Domain.Tests.Services;

public sealed class DefenceSessionMilestoneServiceTests
{
    private readonly DefenceSessionMilestoneService _service = new();

    // TC-DOM-MS-001
    [Fact]
    public void ValidateMilestones_WrongCount_Throws()
    {
        List<(DateOnly DueDate, int ExpectedPercent)> milestones =
        [
            (new DateOnly(2026, 3, 1), 30),
            (new DateOnly(2026, 4, 1), 60),
        ];

        Assert.Throws<DomainException>(() => _service.ValidateMilestones(milestones));
    }

    // TC-DOM-MS-002
    [Fact]
    public void ValidateMilestones_TooManyMilestones_Throws()
    {
        List<(DateOnly DueDate, int ExpectedPercent)> milestones =
        [
            (new DateOnly(2026, 3, 1), 25),
            (new DateOnly(2026, 4, 1), 50),
            (new DateOnly(2026, 5, 1), 75),
            (new DateOnly(2026, 6, 1), 100),
        ];

        Assert.Throws<DomainException>(() => _service.ValidateMilestones(milestones));
    }

    // TC-DOM-MS-003
    [Fact]
    public void ValidateMilestones_EqualDates_Throws()
    {
        List<(DateOnly DueDate, int ExpectedPercent)> milestones =
        [
            (new DateOnly(2026, 3, 1), 30),
            (new DateOnly(2026, 3, 1), 60),
            (new DateOnly(2026, 5, 1), 100),
        ];

        Assert.Throws<DomainException>(() => _service.ValidateMilestones(milestones));
    }

    // TC-DOM-MS-004
    [Fact]
    public void ValidateMilestones_DecreasingDates_Throws()
    {
        List<(DateOnly DueDate, int ExpectedPercent)> milestones =
        [
            (new DateOnly(2026, 4, 1), 30),
            (new DateOnly(2026, 3, 1), 60),
            (new DateOnly(2026, 5, 1), 100),
        ];

        Assert.Throws<DomainException>(() => _service.ValidateMilestones(milestones));
    }

    // TC-DOM-MS-005
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ValidateMilestones_PercentOutOfRange_Throws(int percent)
    {
        List<(DateOnly DueDate, int ExpectedPercent)> milestones =
        [
            (new DateOnly(2026, 3, 1), percent),
            (new DateOnly(2026, 4, 1), 60),
            (new DateOnly(2026, 5, 1), 100),
        ];

        Assert.Throws<DomainException>(() => _service.ValidateMilestones(milestones));
    }

    // TC-DOM-MS-006
    [Fact]
    public void ValidateMilestones_HappyPath_DoesNotThrow()
    {
        List<(DateOnly DueDate, int ExpectedPercent)> milestones =
        [
            (new DateOnly(2026, 3, 1), 30),
            (new DateOnly(2026, 4, 1), 60),
            (new DateOnly(2026, 5, 1), 100),
        ];

        _service.ValidateMilestones(milestones);
    }

    // TC-DOM-MS-007
    [Fact]
    public void ValidateMilestones_NullMilestones_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => _service.ValidateMilestones(null!));
    }

    // TC-DOM-MS-010
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(100)]
    public void ValidateActualPercent_InRange_DoesNotThrow(int percent)
    {
        _service.ValidateActualPercent(percent);
    }

    // TC-DOM-MS-011
    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void ValidateActualPercent_OutOfRange_Throws(int percent)
    {
        Assert.Throws<DomainException>(() => _service.ValidateActualPercent(percent));
    }

    // TC-DOM-MS-020
    [Fact]
    public void EnsureSessionActive_Active_DoesNotThrow()
    {
        _service.EnsureSessionActive(new DefenceSession { Status = DefenceSessionStatus.Active });
    }

    // TC-DOM-MS-021
    [Fact]
    public void EnsureSessionActive_Archived_Throws()
    {
        Assert.Throws<DomainException>(() =>
            _service.EnsureSessionActive(new DefenceSession { Status = DefenceSessionStatus.Archived }));
    }
}

public sealed class DefenceDatePreferenceServiceTests
{
    private readonly DefenceDatePreferenceService _service = new();

    // TC-DOM-DP-001
    [Fact]
    public void EnsureCanRequest_SessionNotActive_Throws()
    {
        Diploma diploma = CreateAdmittedDiploma();
        DefenceSession session = CreateSession(DefenceSessionStatus.Archived);
        DefenceDateOption option = CreateOption(diploma.DefenceSessionId);

        Assert.Throws<DomainException>(() =>
            _service.EnsureCanRequest(diploma, session, option, preferenceAlreadyExists: false));
    }

    // TC-DOM-DP-002
    [Fact]
    public void EnsureCanRequest_NotAdmitted_Throws()
    {
        Diploma diploma = CreateAdmittedDiploma();
        diploma.AdmissionStatus = DiplomaAdmissionStatus.NotAdmitted;
        DefenceSession session = CreateSession(DefenceSessionStatus.Active);
        DefenceDateOption option = CreateOption(diploma.DefenceSessionId);

        Assert.Throws<DomainException>(() =>
            _service.EnsureCanRequest(diploma, session, option, preferenceAlreadyExists: false));
    }

    // TC-DOM-DP-003
    [Fact]
    public void EnsureCanRequest_PreferenceAlreadyExists_Throws()
    {
        Diploma diploma = CreateAdmittedDiploma();
        DefenceSession session = CreateSession(DefenceSessionStatus.Active);
        DefenceDateOption option = CreateOption(diploma.DefenceSessionId);

        Assert.Throws<DomainException>(() =>
            _service.EnsureCanRequest(diploma, session, option, preferenceAlreadyExists: true));
    }

    // TC-DOM-DP-004
    [Fact]
    public void EnsureCanRequest_OptionFromDifferentSession_Throws()
    {
        Diploma diploma = CreateAdmittedDiploma();
        DefenceSession session = CreateSession(DefenceSessionStatus.Active);
        DefenceDateOption option = CreateOption(Guid.NewGuid());

        Assert.Throws<DomainException>(() =>
            _service.EnsureCanRequest(diploma, session, option, preferenceAlreadyExists: false));
    }

    // TC-DOM-DP-005
    [Fact]
    public void EnsureCanRequest_HappyPath_DoesNotThrow()
    {
        Diploma diploma = CreateAdmittedDiploma();
        DefenceSession session = CreateSession(DefenceSessionStatus.Active);
        DefenceDateOption option = CreateOption(diploma.DefenceSessionId);

        _service.EnsureCanRequest(diploma, session, option, preferenceAlreadyExists: false);
    }

    // TC-DOM-DP-010
    [Fact]
    public void EnsureCanRemoveDateOption_HasPreferences_Throws()
    {
        DefenceDateOption option = CreateOption(Guid.NewGuid());

        Assert.Throws<DomainException>(() =>
            _service.EnsureCanRemoveDateOption(option, hasPreferences: true, isAssignedAsFinalDate: false));
    }

    // TC-DOM-DP-011
    [Fact]
    public void EnsureCanRemoveDateOption_AssignedAsFinalDate_Throws()
    {
        DefenceDateOption option = CreateOption(Guid.NewGuid());

        Assert.Throws<DomainException>(() =>
            _service.EnsureCanRemoveDateOption(option, hasPreferences: false, isAssignedAsFinalDate: true));
    }

    // TC-DOM-DP-012
    [Fact]
    public void EnsureCanRemoveDateOption_Unused_DoesNotThrow()
    {
        DefenceDateOption option = CreateOption(Guid.NewGuid());

        _service.EnsureCanRemoveDateOption(option, hasPreferences: false, isAssignedAsFinalDate: false);
    }

    private static Diploma CreateAdmittedDiploma()
    {
        Guid sessionId = Guid.NewGuid();
        return new Diploma
        {
            DefenceSessionId = sessionId,
            AdmissionStatus = DiplomaAdmissionStatus.Admitted,
        };
    }

    private static DefenceSession CreateSession(DefenceSessionStatus status) => new()
    {
        Status = status,
    };

    private static DefenceDateOption CreateOption(Guid defenceSessionId) => new()
    {
        DefenceSessionId = defenceSessionId,
        Date = new DateOnly(2026, 6, 20),
    };
}

public sealed class ConfirmDefenceDateArchivedSessionTests
{
    private readonly DiplomaAdmissionService _service = new();

    // TC-DOM-CD-001
    [Fact]
    public void ConfirmDefenceDate_WhenSessionArchived_Throws()
    {
        Diploma diploma = new()
        {
            AdmissionStatus = DiplomaAdmissionStatus.Admitted,
        };

        DefenceSession session = new() { Status = DefenceSessionStatus.Archived };
        DateOnly date = new(2026, 6, 20);

        Assert.Throws<DomainException>(() =>
            _service.ConfirmDefenceDate(diploma, session, date, [new DefenceDateOption { Date = date }]));
    }
}
