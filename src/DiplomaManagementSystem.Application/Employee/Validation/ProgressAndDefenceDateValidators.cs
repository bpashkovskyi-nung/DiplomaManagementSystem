using DiplomaManagementSystem.Application.Employee.Dtos;

using FluentValidation;

namespace DiplomaManagementSystem.Application.Employee.Validation;

public sealed class SetMilestoneProgressValidator : AbstractValidator<SetMilestoneProgressDto>
{
    public SetMilestoneProgressValidator()
    {
        RuleFor(dto => dto.DiplomaId).NotEmpty();
        RuleFor(dto => dto.MilestoneId).NotEmpty();
        RuleFor(dto => dto.ActualPercent).InclusiveBetween(0, 100);
    }
}

public sealed class RequestDefenceDateValidator : AbstractValidator<RequestDefenceDateDto>
{
    public RequestDefenceDateValidator()
    {
        RuleFor(dto => dto.DiplomaId).NotEmpty();
        RuleFor(dto => dto.DefenceDateOptionId).NotEmpty();
    }
}
