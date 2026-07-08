using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits;
using DiplomaManagementSystem.Application.Admin.EmployeeWorkloadLimits.Dtos;
using DiplomaManagementSystem.Application.Admin.AnnualRoles;
using DiplomaManagementSystem.Application.Admin.AnnualRoles.Dtos;
using DiplomaManagementSystem.Application.Admin.DefenceSessions;
using DiplomaManagementSystem.Application.Admin.DefenceSessions.Dtos;
using DiplomaManagementSystem.Application.Admin.Employees;
using DiplomaManagementSystem.Application.Admin.Employees.Dtos;
using DiplomaManagementSystem.Application.Admin.Students;
using DiplomaManagementSystem.Application.Admin.Students.Dtos;
using DiplomaManagementSystem.Application.Admin.StudyGroups;
using DiplomaManagementSystem.Application.Admin.StudyGroups.Dtos;
using DiplomaManagementSystem.Application.Options;
using DiplomaManagementSystem.Application.Security;
using DiplomaManagementSystem.Domain.Enums;

using FluentValidation.Results;

namespace DiplomaManagementSystem.Application.Tests.Validation;

public sealed class AdminFormValidatorTests
{
    private readonly EmailDomainValidator _emailDomainValidator = new(Microsoft.Extensions.Options.Options.Create(new SecurityOptions
    {
        AllowedEmailDomains = ["test.local"],
    }));

    // TC-APP-ADM-005a
    [Fact]
    public void DefenceSessionFormValidator_YearOutOfRange_Invalid()
    {
        DefenceSessionFormValidator validator = new();
        ValidationResult result = validator.Validate(new DefenceSessionFormDto(null, 1999, DefenceSessionType.Bachelor, 1));

        Assert.False(result.IsValid);
    }

    // TC-APP-ADM-005b
    [Fact]
    public void DefenceSessionFormValidator_ValidYear_Valid()
    {
        DefenceSessionFormValidator validator = new();
        ValidationResult result = validator.Validate(new DefenceSessionFormDto(null, 2026, DefenceSessionType.Bachelor, 1));

        Assert.True(result.IsValid);
    }

    // TC-APP-ADM-005c
    [Fact]
    public void DefenceSessionFormValidator_InvalidSemester_Invalid()
    {
        DefenceSessionFormValidator validator = new();
        ValidationResult result = validator.Validate(new DefenceSessionFormDto(null, 2026, DefenceSessionType.Bachelor, 3));

        Assert.False(result.IsValid);
    }

    // TC-APP-ADM-005d
    [Fact]
    public void AssignAnnualRoleValidator_EmptyIds_Invalid()
    {
        AssignAnnualRoleValidator validator = new();
        ValidationResult result = validator.Validate(
            new AssignAnnualRoleDto(Guid.Empty, AnnualRoleType.DepartmentHead, Guid.Empty));

        Assert.False(result.IsValid);
    }

    // TC-APP-ADM-005e
    [Fact]
    public void StudyGroupFormValidator_EmptyName_Invalid()
    {
        StudyGroupFormValidator validator = new();
        ValidationResult result = validator.Validate(
            new StudyGroupFormDto(null, Guid.NewGuid(), "", Guid.NewGuid(), "очної форми навчання"));

        Assert.False(result.IsValid);
    }

    // TC-APP-ADM-005f
    [Fact]
    public void StudentFormValidator_DisallowedEmailDomain_Invalid()
    {
        StudentFormValidator validator = new(_emailDomainValidator);
        ValidationResult result = validator.Validate(new StudentFormDto(
            null,
            "Студент",
            "student@gmail.com",
            Guid.NewGuid(),
            Guid.NewGuid()));

        Assert.False(result.IsValid);
    }

    // TC-APP-ADM-005g
    [Fact]
    public void StudentFormValidator_AllowedEmailDomain_Valid()
    {
        StudentFormValidator validator = new(_emailDomainValidator);
        ValidationResult result = validator.Validate(new StudentFormDto(
            null,
            "Студент",
            "student@test.local",
            Guid.NewGuid(),
            Guid.NewGuid()));

        Assert.True(result.IsValid);
    }

    // TC-APP-ADM-005h
    [Fact]
    public void EmployeeFormValidator_DisallowedEmailDomain_Invalid()
    {
        EmployeeFormValidator validator = new(_emailDomainValidator);
        ValidationResult result = validator.Validate(new EmployeeFormDto(null, "Петро", "petro@gmail.com"));

        Assert.False(result.IsValid);
    }

    // TC-APP-ADM-005i
    [Fact]
    public void EmployeeFormValidator_Valid_Valid()
    {
        EmployeeFormValidator validator = new(_emailDomainValidator);
        ValidationResult result = validator.Validate(new EmployeeFormDto(null, "Петро Петренко", "petro@test.local"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void SetEmployeeWorkloadLimitValidator_NegativeLimit_Invalid()
    {
        SetEmployeeWorkloadLimitValidator validator = new();
        ValidationResult result = validator.Validate(
            new SetEmployeeWorkloadLimitDto(Guid.NewGuid(), Guid.NewGuid(), -1, 0));

        Assert.False(result.IsValid);
    }

    [Fact]
    public void SetEmployeeWorkloadLimitValidator_ValidLimits_Valid()
    {
        SetEmployeeWorkloadLimitValidator validator = new();
        ValidationResult result = validator.Validate(
            new SetEmployeeWorkloadLimitDto(Guid.NewGuid(), Guid.NewGuid(), 3, 2));

        Assert.True(result.IsValid);
    }
}
