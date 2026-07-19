using DiplomaManagementSystem.Application.Secretary.Dtos;

using FluentValidation;

namespace DiplomaManagementSystem.Application.Secretary.Validation;

public sealed class SaveMilestonesValidator : AbstractValidator<SaveMilestonesDto>
{
    public SaveMilestonesValidator()
    {
        RuleFor(dto => dto.Milestones)
            .NotNull()
            .Must(items => items.Count == 3)
            .WithMessage("Exactly three milestones are required.");

        RuleForEach(dto => dto.Milestones).ChildRules(item =>
        {
            item.RuleFor(x => x.DueDate).NotEmpty();
            item.RuleFor(x => x.ExpectedPercent).InclusiveBetween(0, 100);
        });
    }
}

public sealed class SaveDefenceDatesValidator : AbstractValidator<SaveDefenceDatesDto>
{
    public SaveDefenceDatesValidator()
    {
        RuleFor(dto => dto.Dates).NotNull();
        RuleForEach(dto => dto.Dates).NotEmpty();
    }
}
