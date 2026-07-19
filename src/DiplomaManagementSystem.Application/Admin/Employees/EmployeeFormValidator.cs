using DiplomaManagementSystem.Application.Admin.Employees.Dtos;
using DiplomaManagementSystem.Application.Security;

using FluentValidation;

namespace DiplomaManagementSystem.Application.Admin.Employees;

public sealed class EmployeeFormValidator : AbstractValidator<EmployeeFormDto>
{
    public EmployeeFormValidator(EmailDomainValidator emailDomainValidator)
    {
        RuleFor(dto => dto.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(dto => dto.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256)
            .Must(emailDomainValidator.IsAllowed)
            .WithMessage("Email domain is not allowed.");

        RuleFor(dto => dto.ShortDisplayName)
            .MaximumLength(64);

        RuleFor(dto => dto.AcademicRank)
            .IsInEnum()
            .When(dto => dto.AcademicRank.HasValue);
    }
}
