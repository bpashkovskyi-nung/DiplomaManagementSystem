using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;
using FluentValidation;

namespace DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits;

public sealed class SetEmployeeWorkloadLimitValidator : AbstractValidator<SetEmployeeWorkloadLimitDto>
{
    public SetEmployeeWorkloadLimitValidator()
    {
        RuleFor(dto => dto.DefenceSessionId).NotEmpty();
        RuleFor(dto => dto.EmployeeId).NotEmpty();
        RuleFor(dto => dto.MaxSupervisorStudents)
            .GreaterThanOrEqualTo(0)
            .When(dto => dto.MaxSupervisorStudents.HasValue);
        RuleFor(dto => dto.MaxReviewerStudents)
            .GreaterThanOrEqualTo(0)
            .When(dto => dto.MaxReviewerStudents.HasValue);
    }
}
