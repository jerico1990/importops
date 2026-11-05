using ImportOps.Application.Common;

namespace ImportOps.Application.Documents;

public interface IDocumentService
{
    Task<PagedResult<DocumentDto>> GetAsync(DocumentQuery query, CancellationToken ct);
    Task<DocumentDto?> GetByIdAsync(long id, CancellationToken ct);
    Task<IReadOnlyCollection<DocumentObservationDto>> GetObservationsAsync(long documentId, CancellationToken ct);
    Task<(Stream Stream, string FileName, string ContentType)?> DownloadAsync(long id, CancellationToken ct);

    Task<DocumentDto> UploadAsync(
        UploadDocumentRequest request,
        Stream stream,
        string originalFileName,
        string contentType,
        long fileSize,
        CancellationToken ct);

    Task<DocumentDto?> ReplaceAsync(
        long documentId,
        Stream stream,
        string originalFileName,
        string contentType,
        long fileSize,
        string? notes,
        CancellationToken ct);

    Task<DocumentDto?> ValidateAsync(long id, ValidateDocumentRequest request, CancellationToken ct);
    Task<DocumentObservationDto?> ObserveAsync(long id, ObserveDocumentRequest request, CancellationToken ct);
    Task<DocumentObservationDto?> ResolveObservationAsync(long id, long observationId, ResolveDocumentObservationRequest request, CancellationToken ct);
    Task<DocumentDto?> VoidAsync(long id, VoidDocumentRequest request, CancellationToken ct);

    Task<OperationDocumentStatusDto> GetOperationRequirementsAsync(long operationId, CancellationToken ct);
    Task<OperationDocumentStatusDto> RefreshOperationDocumentaryStatusAsync(long operationId, CancellationToken ct);
}
