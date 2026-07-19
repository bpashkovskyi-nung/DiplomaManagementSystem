using System.Security.Claims;

using DiplomaManagementSystem.Application;
using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Employee.Contracts;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Application.Student.Contracts;
using DiplomaManagementSystem.Application.Student.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Student.Models;
using DiplomaManagementSystem.Web.Mapping;
using DiplomaManagementSystem.Web.Storage;

using FluentValidation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Student.Controllers;

[Area("Student")]
[Authorize(Roles = RoleNames.Student)]
public sealed class DiplomaController(
    IStudentDiplomaService studentDiplomaService,
    IDefenceDateRequestService defenceDateRequestService,
    IValidator<SelectSupervisorDto> selectSupervisorValidator,
    IValidator<SubmitTopicDto> submitTopicValidator,
    IValidator<RequestDefenceDateDto> defenceDateRequestValidator) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        Guid studentId = GetUserId();
        MyDiplomaDto dto = await studentDiplomaService.GetMyDiplomaAsync(studentId, cancellationToken);
        MyDiplomaViewModel model = StudentDiplomaViewModelMapper.Map(dto);
        model.DefenceDateRequest =
            await defenceDateRequestService.GetFormForStudentAsync(studentId, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestDefenceDate(
        RequestDefenceDateDto request,
        CancellationToken cancellationToken)
    {
        FluentValidation.Results.ValidationResult validation =
            await defenceDateRequestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await defenceDateRequestService.RequestAsStudentAsync(GetUserId(), request, cancellationToken);
            TempData["Success"] = "Побажання щодо дати захисту надіслано.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectSupervisor(SelectSupervisorViewModel model, CancellationToken cancellationToken)
    {
        SelectSupervisorDto dto = new(model.DiplomaId, model.SupervisorId);
        FluentValidation.Results.ValidationResult validation = await selectSupervisorValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await studentDiplomaService.SelectSupervisorAsync(GetUserId(), dto, cancellationToken);
            TempData["Success"] = "Запит на керівника надіслано.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> SubmitTopic(CancellationToken cancellationToken)
    {
        Guid studentId = GetUserId();
        MyDiplomaDto dto = await studentDiplomaService.GetMyDiplomaAsync(studentId, cancellationToken);
        if (!dto.Header.HasDiploma
            || dto.Actions?.CanSubmitTopic != true
            || dto.Header.DiplomaId is null)
        {
            return RedirectToAction(nameof(Index));
        }

        SubmitTopicViewModel model = new()
        {
            DiplomaId = dto.Header.DiplomaId.Value,
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitTopic(SubmitTopicViewModel model, CancellationToken cancellationToken)
    {
        SubmitTopicDto dto = new(model.DiplomaId, model.Title);
        FluentValidation.Results.ValidationResult validation = await submitTopicValidator.ValidateAsync(dto, cancellationToken);
        if (!validation.IsValid)
        {
            foreach (FluentValidation.Results.ValidationFailure error in validation.Errors)
            {
                ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
            }

            return View(model);
        }

        try
        {
            await studentDiplomaService.SubmitTopicAsync(GetUserId(), dto, cancellationToken);
            MyDiplomaDto updated = await studentDiplomaService.GetMyDiplomaAsync(GetUserId(), cancellationToken);
            TempData["Success"] = updated.Header.SessionType.HasValue
                ? DefenceWorkLabel.TopicSubmitted(updated.Header.SessionType.Value)
                : "Тему роботи подано на розгляд.";
            return RedirectToAction(nameof(Index));
        }
        catch (DomainException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclareWorkReady(CancellationToken cancellationToken)
    {
        Guid studentId = GetUserId();
        MyDiplomaDto dto = await studentDiplomaService.GetMyDiplomaAsync(studentId, cancellationToken);
        if (dto.Header.DiplomaId is null)
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await studentDiplomaService.DeclareWorkReadyAsync(studentId, dto.Header.DiplomaId.Value, cancellationToken);
            TempData["Success"] = "Роботу передано на перевірки. Контрольні точки розпочато.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadWork(UploadWorkViewModel model, CancellationToken cancellationToken)
    {
        if (model.WorkFile is null || model.WorkFile.Length == 0)
        {
            TempData["Error"] = "Оберіть файл роботи (PDF, DOCX або ODT).";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            UploadFileContent content = UploadFileMapper.ToUploadContent(model.WorkFile);
            await studentDiplomaService.UploadWorkAsync(GetUserId(), model.DiplomaId, content, cancellationToken);
            TempData["Success"] = "Файл роботи завантажено.";
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private Guid GetUserId()
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing.");
        }

        return userId;
    }

}
