using DiplomaManagementSystem.Application.Secretary.Documents.Dtos;

using FluentValidation;

namespace DiplomaManagementSystem.Application.Secretary.Documents;

public sealed class TopicOrderGenerateRequestValidator : AbstractValidator<TopicOrderGenerateRequestDto>
{
    public TopicOrderGenerateRequestValidator()
    {
        RuleFor(request => request.OrderNumber)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(request => request.Year)
            .InclusiveBetween(2000, 2100);

        RuleFor(request => request.StudyGroupIds)
            .NotEmpty()
            .WithMessage("Оберіть хоча б одну групу.");
    }
}
