using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Web.Areas.SuperAdmin;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Validation;
using DiplomaManagementSystem.Web.Tests.Support;

using FluentValidation.TestHelper;

namespace DiplomaManagementSystem.Web.Tests.SuperAdmin;

public sealed class SuperAdminNavigationTests
{
    [Fact]
    public void SuperAdminNavigation_FacultyDepartmentsBack_ReturnsFacultiesLinkOnly()
    {
        IReadOnlyList<SuperAdminNavLink> links = SuperAdminNavigation.FacultyDepartmentsBack();

        Assert.Single(links);
        Assert.Equal(SuperAdminPageTitles.Faculties, links[0].Text);
        Assert.Equal("Faculties", links[0].Controller);
    }

    [Fact]
    public void SuperAdminNavigation_DepartmentFormBack_IncludesFacultiesAndDepartments()
    {
        var facultyId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        IReadOnlyList<SuperAdminNavLink> links = SuperAdminNavigation.DepartmentFormBack(facultyId, "ІТ");

        Assert.Equal(2, links.Count);
        Assert.Equal(SuperAdminPageTitles.Faculties, links[0].Text);
        Assert.Equal("ІТ", links[1].Text);
        Assert.Equal("Departments", links[1].Controller);
        Assert.Equal(facultyId.ToString(), links[1].RouteValues!["facultyId"]);
    }
}

public sealed class OrganizationImportViewModelValidatorTests
{
    private readonly OrganizationImportViewModelValidator _validator = new();

    [Fact]
    public void Validate_WhenFileMissing_HasError()
    {
        OrganizationImportViewModel model = new();

        TestValidationResult<OrganizationImportViewModel> result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(viewModel => viewModel.File);
    }

    [Fact]
    public void Validate_WhenFileIsNotJson_HasError()
    {
        OrganizationImportViewModel model = new()
        {
            File = new FakeFormFile("structure.txt", System.Text.Encoding.UTF8.GetBytes("[]"), "text/plain"),
        };

        TestValidationResult<OrganizationImportViewModel> result = _validator.TestValidate(model);

        result.ShouldHaveValidationErrorFor(viewModel => viewModel.File);
    }

    [Fact]
    public void Validate_WhenJsonFileProvided_IsValid()
    {
        OrganizationImportViewModel model = new()
        {
            File = new FakeFormFile("structure.json", System.Text.Encoding.UTF8.GetBytes("[]"), "application/json"),
        };

        TestValidationResult<OrganizationImportViewModel> result = _validator.TestValidate(model);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
