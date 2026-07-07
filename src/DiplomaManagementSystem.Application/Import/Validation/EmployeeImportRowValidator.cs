using DiplomaManagementSystem.Application.Common;
using DiplomaManagementSystem.Application.Import.Models;

using FluentValidation;

namespace DiplomaManagementSystem.Application.Import.Validation;

public sealed class EmployeeImportRowValidator : AbstractValidator<EmployeeImportRow>
{
    public EmployeeImportRowValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.ShortDisplayName)
            .MaximumLength(64);

        RuleFor(x => x.AcademicRankRaw)
            .Must(rank => string.IsNullOrWhiteSpace(rank) || AcademicRankLabels.TryParse(rank, out _))
            .WithMessage("Невідоме вчене звання.");
    }
}
