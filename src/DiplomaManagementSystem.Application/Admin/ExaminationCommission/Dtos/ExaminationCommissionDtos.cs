using DiplomaManagementSystem.Domain.Enums;

namespace DiplomaManagementSystem.Application.Admin.ExaminationCommission.Dtos;

public sealed record CommissionEmployeeOptionDto(
    Guid Id,
    string FullName,
    string Email,
    string? Position);

public sealed record ExaminationCommissionParticipantDto(
    ExaminationCommissionRole Role,
    Guid? EmployeeId,
    string FullName,
    string Position,
    int SortOrder);

public sealed record ExaminationCommissionDto(
    ExaminationCommissionParticipantDto? Chair,
    IReadOnlyList<ExaminationCommissionParticipantDto> Members);

public sealed record ExaminationCommissionEditorDto(
    Guid DefenceSessionId,
    string SessionLabel,
    ExaminationCommissionDto Commission,
    IReadOnlyList<CommissionEmployeeOptionDto> Employees);

public sealed record SaveExaminationCommissionParticipantDto(
    bool IsExternal,
    Guid? EmployeeId,
    string? FullName,
    string? Position);

public sealed record SaveExaminationCommissionDto(
    Guid DefenceSessionId,
    SaveExaminationCommissionParticipantDto Chair,
    IReadOnlyList<SaveExaminationCommissionParticipantDto> Members);
