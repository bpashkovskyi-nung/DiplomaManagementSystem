using DiplomaManagementSystem.Application.Secretary.Dtos;
using DiplomaManagementSystem.Domain;

using FluentValidation;

namespace DiplomaManagementSystem.Application.Secretary.Validation;

public sealed class AssignReviewerValidator : AbstractValidator<AssignReviewerDto>
{
    public AssignReviewerValidator()
    {
        RuleFor(dto => dto.DiplomaId).NotEmpty();
        RuleFor(dto => dto.ReviewerId).NotEmpty();
    }
}

public sealed class AdmitDiplomaValidator : AbstractValidator<AdmitDiplomaDto>
{
    public AdmitDiplomaValidator()
    {
        RuleFor(dto => dto.DiplomaId).NotEmpty();
    }
}

public sealed class ConfirmDefenceDateValidator : AbstractValidator<ConfirmDefenceDateDto>
{
    public ConfirmDefenceDateValidator()
    {
        RuleFor(dto => dto.DiplomaId).NotEmpty();
        RuleFor(dto => dto.DefenceDate).NotEmpty();
    }
}

public sealed class OverrideSupervisorValidator : AbstractValidator<OverrideSupervisorDto>
{
    public OverrideSupervisorValidator()
    {
        RuleFor(dto => dto.DiplomaId).NotEmpty();
        RuleFor(dto => dto.SupervisorId).NotEmpty();
        RuleFor(dto => dto.Reason)
            .NotEmpty()
            .MaximumLength(500);
    }
}

public sealed class AddCommentValidator : AbstractValidator<AddCommentDto>
{
    public AddCommentValidator()
    {
        RuleFor(dto => dto.DiplomaId).NotEmpty();
        RuleFor(dto => dto.Body)
            .NotEmpty()
            .MaximumLength(2000);
    }
}

public sealed class OverrideAdmissionStepValidator : AbstractValidator<OverrideAdmissionStepDto>
{
    public OverrideAdmissionStepValidator()
    {
        RuleFor(dto => dto.DiplomaId).NotEmpty();
        RuleFor(dto => dto.Step).IsInEnum();
        RuleFor(dto => dto.Outcome).IsInEnum();
        RuleFor(dto => dto.Comment)
            .NotEmpty()
            .When(dto => CheckpointOutcomeRules.RequiresComment(dto.Outcome))
            .MaximumLength(2000);
    }
}
