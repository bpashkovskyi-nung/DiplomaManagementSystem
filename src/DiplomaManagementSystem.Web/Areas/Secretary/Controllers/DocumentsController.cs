using DiplomaManagementSystem.Application.Secretary.Contracts;
using DiplomaManagementSystem.Application.Secretary.Documents.Contracts;
using DiplomaManagementSystem.Application.Secretary.Documents.Dtos;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Secretary.Models;
using DiplomaManagementSystem.Web.Extensions;
using DiplomaManagementSystem.Web.Secretary;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DiplomaManagementSystem.Web.Areas.Secretary.Controllers;

public sealed class DocumentsController(
    ISecretarySessionService sessionService,
    ISecretaryAccessService accessService,
    ITopicOrderDocumentService topicOrderDocumentService,
    IValidator<TopicOrderGenerateRequestDto> validator) : SecretaryControllerBase(sessionService, accessService)
{
    [HttpGet]
    public async Task<IActionResult> TopicOrder(CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        TopicOrderFormDto? form = await topicOrderDocumentService.GetFormAsync(sessionId, cancellationToken);
        if (form is null)
        {
            return RedirectToAction("Select", "Session");
        }

        return View(MapForm(form));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TopicOrder(TopicOrderViewModel model, CancellationToken cancellationToken)
    {
        (Guid sessionId, IActionResult? redirect) = await ResolveSessionAsync(cancellationToken);
        if (redirect is not null)
        {
            return redirect;
        }

        model.SessionId = sessionId;
        TopicOrderGenerateRequestDto request = new(
            sessionId,
            model.OrderNumber,
            model.Year,
            model.SelectedStudyGroupIds);

        if (!await this.TryValidateFormAsync(validator, request, cancellationToken))
        {
            await ReloadFormAsync(model, cancellationToken);
            return View(model);
        }

        try
        {
            byte[]? content = await topicOrderDocumentService.ExportDocxAsync(request, cancellationToken);
            if (content is null || content.Length == 0)
            {
                TopicOrderPreviewDto? preview = await topicOrderDocumentService.BuildPreviewAsync(request, cancellationToken);
                model.Warnings = preview?.Document.Warnings.ToList() ?? ["Не вдалося згенерувати документ."];
                await ReloadFormAsync(model, cancellationToken);
                ModelState.AddModelError(string.Empty, "Немає даних для наказу. Перевірте групи та статуси тем.");
                return View(model);
            }

            string fileName = $"nakaz-temy-{model.OrderNumber}.docx";
            return File(
                content,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileName);
        }
        catch (DomainException ex)
        {
            await ReloadFormAsync(model, cancellationToken);
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    private async Task ReloadFormAsync(TopicOrderViewModel model, CancellationToken cancellationToken)
    {
        TopicOrderFormDto? form = await topicOrderDocumentService.GetFormAsync(model.SessionId, cancellationToken);
        if (form is null)
        {
            return;
        }

        model.SessionLabel = form.SessionLabel;
        model.StudyGroups = form.StudyGroups
            .Select(group => new TopicOrderStudyGroupOptionViewModel
            {
                Id = group.Id,
                Name = group.Name,
                Course = group.Course,
            })
            .ToList();
    }

    private static TopicOrderViewModel MapForm(TopicOrderFormDto form) =>
        new()
        {
            SessionId = form.SessionId,
            SessionLabel = form.SessionLabel,
            Year = form.DefaultYear,
            StudyGroups = form.StudyGroups
                .Select(group => new TopicOrderStudyGroupOptionViewModel
                {
                    Id = group.Id,
                    Name = group.Name,
                    Course = group.Course,
                })
                .ToList(),
        };
}
