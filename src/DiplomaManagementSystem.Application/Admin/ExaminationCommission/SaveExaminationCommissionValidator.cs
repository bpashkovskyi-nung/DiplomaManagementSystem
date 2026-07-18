using DiplomaManagementSystem.Application.Admin.ExaminationCommission.Dtos;

using FluentValidation;

namespace DiplomaManagementSystem.Application.Admin.ExaminationCommission;

public sealed class SaveExaminationCommissionValidator : AbstractValidator<SaveExaminationCommissionDto>
{
    public const int MinimumMemberCount = 3;
    public const int MaxFullNameLength = 200;
    public const int MaxPositionLength = 300;

    public SaveExaminationCommissionValidator()
    {
        RuleFor(x => x.DefenceSessionId).NotEmpty();
        RuleFor(x => x.Chair).NotNull();
        RuleFor(x => x.Members).NotNull();
        RuleFor(x => x.Members)
            .Must(members => members.Count >= MinimumMemberCount)
            .WithMessage($"Склад ЕК повинен містити щонайменше {MinimumMemberCount} членів.");

        RuleFor(x => x.Chair).SetValidator(new SaveExaminationCommissionParticipantValidator("Голова ЕК"));
        RuleForEach(x => x.Members).SetValidator(new SaveExaminationCommissionParticipantValidator("Член ЕК"));
    }
}

internal sealed class SaveExaminationCommissionParticipantValidator
    : AbstractValidator<SaveExaminationCommissionParticipantDto>
{
    public SaveExaminationCommissionParticipantValidator(string roleLabel)
    {
        When(
            x => x.IsExternal,
            () =>
            {
                RuleFor(x => x.FullName)
                    .NotEmpty()
                    .WithMessage($"{roleLabel}: вкажіть ПІБ.")
                    .MaximumLength(SaveExaminationCommissionValidator.MaxFullNameLength);

                RuleFor(x => x.Position)
                    .NotEmpty()
                    .WithMessage($"{roleLabel}: вкажіть посаду.")
                    .MaximumLength(SaveExaminationCommissionValidator.MaxPositionLength);

                RuleFor(x => x.EmployeeId)
                    .Empty()
                    .WithMessage($"{roleLabel}: для зовнішньої особи не можна обирати викладача.");
            });

        When(
            x => !x.IsExternal,
            () =>
            {
                RuleFor(x => x.EmployeeId)
                    .NotEmpty()
                    .WithMessage($"{roleLabel}: оберіть викладача кафедри.");
            });
    }
}
