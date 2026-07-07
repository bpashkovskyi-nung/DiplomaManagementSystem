using DiplomaManagementSystem.Application.Admin.StudyGroups.Dtos;

using FluentValidation;

namespace DiplomaManagementSystem.Application.Admin.StudyGroups;

public sealed class StudyGroupFormValidator : AbstractValidator<StudyGroupFormDto>
{
    public StudyGroupFormValidator()
    {
        RuleFor(dto => dto.DefenceSessionId)
            .NotEmpty();

        RuleFor(dto => dto.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(dto => dto.Course)
            .InclusiveBetween(1, 6)
            .When(dto => dto.Course.HasValue);
    }
}
