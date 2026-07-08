using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;

using FluentValidation;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin.Validation;

internal sealed class OrganizationImportViewModelValidator : AbstractValidator<OrganizationImportViewModel>
{
    public OrganizationImportViewModelValidator()
    {
        RuleFor(model => model.File)
            .NotNull()
            .WithMessage("Оберіть JSON-файл.");

        RuleFor(model => model.File)
            .Must(file => file is not null && string.Equals(Path.GetExtension(file.FileName), ".json", StringComparison.OrdinalIgnoreCase))
            .When(model => model.File is not null)
            .WithMessage("Дозволений лише формат .json.");
    }
}
