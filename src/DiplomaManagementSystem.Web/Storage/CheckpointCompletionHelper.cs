using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Domain;
using DiplomaManagementSystem.Web.Areas.Employee.Models;

namespace DiplomaManagementSystem.Web.Storage;

internal static class CheckpointCompletionHelper
{
    public static bool TryGetRequiredDocument(
        CompleteCheckpointViewModel model,
        out UploadFileContent? content,
        out string? errorMessage)
    {
        if (!RequiresDocumentUpload(model))
        {
            content = null;
            errorMessage = null;
            return true;
        }

        if (model.Document is null || model.Document.Length == 0)
        {
            content = null;
            errorMessage = "Обов'язково додайте файл (PDF, DOCX або ODT).";
            return false;
        }

        content = UploadFileMapper.ToUploadContent(model.Document);
        errorMessage = null;
        return true;
    }

    public static bool RequiresDocumentUpload(CompleteCheckpointViewModel model) =>
        model.RequiresDocumentFile && CheckpointOutcomeRules.RequiresDocument(model.Outcome);
}
