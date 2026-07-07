using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Persistence;

public sealed record StudentDisplayInfo(string FullName, string GroupName);

public sealed record DefenceSessionSummary(
    Guid Id,
    int Year,
    DefenceSessionType Type,
    int? Semester);

public sealed record DiplomaDashboardState(
    DiplomaLifecycleStatus LifecycleStatus,
    AdmissionStep? CurrentAdmissionStep);

public sealed record UserOption(Guid Id, string FullName, string Email);

public sealed record StudyGroupOption(Guid Id, string Name, int? Course = null);

public sealed record SecretarySessionRow(
    Guid Id,
    int Year,
    DefenceSessionType Type,
    int? Semester);

public sealed record StudentStorageContext(
    Guid StudentId,
    Guid StudyGroupId,
    string StudyGroupName,
    string StudentFullName);
