using DiplomaManagementSystem.Application.Storage;
using DiplomaManagementSystem.Domain.Enums;
using DiplomaManagementSystem.Domain.Exceptions;
using DiplomaManagementSystem.Web.Areas.Employee.Models;
using DiplomaManagementSystem.Web.Storage;
using DiplomaManagementSystem.Web.Tests.Support;

namespace DiplomaManagementSystem.Web.Tests.Storage;

public sealed class UploadAndCheckpointHelperTests
{
    // TC-WEB-MAP-011
    [Fact]
    public void ToUploadContent_MapsFormFile()
    {
        FakeFormFile file = new("report.pdf", [1, 2, 3]);

        UploadFileContent content = UploadFileMapper.ToUploadContent(file);

        Assert.Equal("report.pdf", content.FileName);
        Assert.Equal("application/pdf", content.ContentType);
        Assert.Equal(3, content.Length);
    }

    // TC-WEB-MAP-011b
    [Fact]
    public void ToUploadContent_WhenEmpty_Throws()
    {
        FakeFormFile file = new("empty.pdf", []);

        Assert.Throws<DomainException>(() => UploadFileMapper.ToUploadContent(file));
    }

    // TC-WEB-MAP-012
    [Fact]
    public void TryGetRequiredDocument_WhenNotRequired_ReturnsTrueWithoutContent()
    {
        CompleteCheckpointViewModel model = new()
        {
            RequiresDocumentFile = false,
        };

        bool success = CheckpointCompletionHelper.TryGetRequiredDocument(model, out UploadFileContent? content, out string? error);

        Assert.True(success);
        Assert.Null(content);
        Assert.Null(error);
    }

    // TC-WEB-MAP-012b
    [Fact]
    public void TryGetRequiredDocument_WhenMissingFile_ReturnsError()
    {
        CompleteCheckpointViewModel model = new()
        {
            RequiresDocumentFile = true,
            Outcome = CheckpointOutcome.Approved,
            Document = null,
        };

        bool success = CheckpointCompletionHelper.TryGetRequiredDocument(model, out _, out string? error);

        Assert.False(success);
        Assert.Contains("PDF", error, StringComparison.Ordinal);
    }

    [Fact]
    public void TryGetRequiredDocument_WhenNotApproved_ReturnsTrueWithoutContent()
    {
        CompleteCheckpointViewModel model = new()
        {
            RequiresDocumentFile = true,
            Outcome = CheckpointOutcome.NotApproved,
            Document = null,
        };

        bool success = CheckpointCompletionHelper.TryGetRequiredDocument(model, out UploadFileContent? content, out string? error);

        Assert.True(success);
        Assert.Null(content);
        Assert.Null(error);
    }

    // TC-WEB-MAP-012c
    [Fact]
    public void TryGetRequiredDocument_WhenFilePresent_ReturnsContent()
    {
        CompleteCheckpointViewModel model = new()
        {
            RequiresDocumentFile = true,
            Outcome = CheckpointOutcome.Approved,
            Document = new FakeFormFile("feedback.pdf", [1, 2]),
        };

        bool success = CheckpointCompletionHelper.TryGetRequiredDocument(model, out UploadFileContent? content, out string? error);

        Assert.True(success);
        Assert.NotNull(content);
        Assert.Null(error);
        Assert.Equal("feedback.pdf", content!.FileName);
    }
}
