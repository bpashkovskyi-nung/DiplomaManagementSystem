using DiplomaManagementSystem.Application.Secretary.Documents.Dtos;

namespace DiplomaManagementSystem.Application.Secretary.Documents.Contracts;

public interface ITopicOrderDocumentService
{
    Task<TopicOrderFormDto?> GetFormAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<TopicOrderPreviewDto?> BuildPreviewAsync(
        TopicOrderGenerateRequestDto request,
        CancellationToken cancellationToken = default);

    Task<byte[]?> ExportDocxAsync(
        TopicOrderGenerateRequestDto request,
        CancellationToken cancellationToken = default);
}
