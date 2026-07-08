using System.Security.Claims;

using DiplomaManagementSystem.Application.Constants;
using DiplomaManagementSystem.Application.Employee.Dtos;
using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Employee.Models;
using DiplomaManagementSystem.Web.Departments;
using DiplomaManagementSystem.Web.Storage;

using FluentValidation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Employee.Controllers;

[Area("Employee")]
[Authorize(Roles = RoleNames.Employee)]
[ServiceFilter(typeof(EmployeeDepartmentContextFilter))]
public abstract class EmployeeControllerBase : Controller
{
    protected Guid GetUserId()
    {
        string? userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdValue is null || !Guid.TryParse(userIdValue, out Guid userId))
        {
            throw new InvalidOperationException("Authenticated user id is missing.");
        }

        return userId;
    }

    protected async Task<IActionResult> CompleteCheckpointAsync(
        CompleteCheckpointViewModel model,
        IValidator<CompleteCheckpointDto> validator,
        Func<Guid, CompleteCheckpointDto, CancellationToken, Task> completeAction,
        string successMessage,
        string redirectAction,
        CancellationToken cancellationToken)
    {
        CompleteCheckpointDto dto = new(model.DiplomaId, model.Outcome, model.Comment);
        FluentValidation.Results.ValidationResult validation =
            await validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return RedirectToAction(redirectAction);
        }

        try
        {
            await completeAction(GetUserId(), dto, cancellationToken);
            TempData["Success"] = successMessage;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(redirectAction);
    }

    protected async Task<IActionResult> CompleteCheckpointWithDocumentAsync(
        CompleteCheckpointViewModel model,
        IValidator<CompleteCheckpointDto> validator,
        Func<Guid, CompleteCheckpointDto, UploadFileContent?, CancellationToken, Task> completeAction,
        string successMessage,
        string redirectAction,
        CancellationToken cancellationToken)
    {
        CompleteCheckpointDto dto = new(model.DiplomaId, model.Outcome, model.Comment);
        FluentValidation.Results.ValidationResult validation =
            await validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return RedirectToAction(redirectAction);
        }

        if (!CheckpointCompletionHelper.TryGetRequiredDocument(model, out UploadFileContent? document, out string? fileError))
        {
            TempData["Error"] = fileError;
            return RedirectToAction(redirectAction);
        }

        try
        {
            await completeAction(GetUserId(), dto, document, cancellationToken);
            TempData["Success"] = successMessage;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(redirectAction);
    }

    protected async Task<IActionResult> ApproveTopicAsync(
        ApproveTopicViewModel model,
        IValidator<ApproveTopicDto> validator,
        Func<Guid, ApproveTopicDto, CancellationToken, Task> approveAction,
        string successMessage,
        string redirectAction,
        CancellationToken cancellationToken)
    {
        ApproveTopicDto dto = new(model.VersionId, model.Comment);
        FluentValidation.Results.ValidationResult validation =
            await validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return RedirectToAction(redirectAction);
        }

        try
        {
            await approveAction(GetUserId(), dto, cancellationToken);
            TempData["Success"] = successMessage;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(redirectAction);
    }

    protected async Task<IActionResult> RejectTopicAsync(
        ReviewTopicViewModel model,
        IValidator<ReviewTopicDto> validator,
        Func<Guid, ReviewTopicDto, CancellationToken, Task> rejectAction,
        string successMessage,
        string redirectAction,
        CancellationToken cancellationToken)
    {
        ReviewTopicDto dto = new(model.VersionId, model.RejectionReason);
        FluentValidation.Results.ValidationResult validation =
            await validator.ValidateAsync(dto, cancellationToken);

        if (!validation.IsValid)
        {
            TempData["Error"] = string.Join(" ", validation.Errors.Select(error => error.ErrorMessage));
            return RedirectToAction(redirectAction);
        }

        try
        {
            await rejectAction(GetUserId(), dto, cancellationToken);
            TempData["Success"] = successMessage;
        }
        catch (DomainException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(redirectAction);
    }
}
