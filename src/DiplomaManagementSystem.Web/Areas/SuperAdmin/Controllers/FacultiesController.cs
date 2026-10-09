using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Contracts;
using DiplomaManagementSystem.Application.SuperAdmin.Faculties.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.SuperAdmin.Models;
using DiplomaManagementSystem.Web.Mapping;

using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.SuperAdmin.Controllers;

public sealed class FacultiesController(IFacultyAdminService facultyAdminService) : SuperAdminControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<FacultyListItemDto> items = await facultyAdminService.GetAllAsync(cancellationToken);
        FacultyListViewModel model = new()
        {
            Items = items.Select(SuperAdminViewModelMapper.Map).ToList(),
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View("Form", new FacultyFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(FacultyFormViewModel model, CancellationToken cancellationToken)
    {
        if (!TryValidateModel(model))
        {
            return View("Form", model);
        }

        try
        {
            await facultyAdminService.CreateAsync(new FacultyFormDto(null, model.Name.Trim()), cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        FacultyFormDto? dto = await facultyAdminService.GetForEditAsync(id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return View("Form", new FacultyFormViewModel
        {
            Id = dto.Id,
            Name = dto.Name,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, FacultyFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!TryValidateModel(model))
        {
            return View("Form", model);
        }

        try
        {
            await facultyAdminService.UpdateAsync(id, new FacultyFormDto(id, model.Name.Trim()), cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await facultyAdminService.DeactivateAsync(id, cancellationToken);
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
