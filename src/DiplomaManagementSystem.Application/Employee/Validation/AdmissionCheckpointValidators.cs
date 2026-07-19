using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Domain;

using FluentValidation;

namespace DiplomaManagementSystem.Application.Employee.Validation;

public sealed class CompleteCheckpointValidator : AbstractValidator<CompleteCheckpointDto>
{
    public CompleteCheckpointValidator()
    {
        RuleFor(dto => dto.DiplomaId).NotEmpty();
        RuleFor(dto => dto.Outcome).IsInEnum();
        RuleFor(dto => dto.Comment)
            .NotEmpty()
            .When(dto => CheckpointOutcomeRules.RequiresComment(dto.Outcome))
            .MaximumLength(1000);
    }
}
