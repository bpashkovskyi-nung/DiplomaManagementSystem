using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.OrganizationImport.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Validation;
using DiplomaManagementSystem.Web.Mapping;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin.Controllers;

public sealed class OrganizationImportController(
    IOrganizationStructureImportService importService,
    IValidator<OrganizationImportViewModel> validator) : SuperAdminControllerBase
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new OrganizationImportViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(OrganizationImportViewModel model, CancellationToken cancellationToken)
    {
        if (!await TryValidateImportAsync(model, cancellationToken))
        {
            return View(model);
        }

        try
        {
            await using Stream stream = model.File!.OpenReadStream();
            OrganizationStructureImportResultDto result =
                await importService.ImportAsync(stream, model.Mode, cancellationToken);
            model.Result = SuperAdminViewModelMapper.Map(result);
            return View(model);
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private async Task<bool> TryValidateImportAsync(
        OrganizationImportViewModel model,
        CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validationResult =
            await validator.ValidateAsync(model, cancellationToken);

        foreach (FluentValidation.Results.ValidationFailure failure in validationResult.Errors)
        {
            ModelState.AddModelError(failure.PropertyName, failure.ErrorMessage);
        }

        return validationResult.IsValid;
    }
}
