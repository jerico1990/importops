using Microsoft.AspNetCore.Http;

namespace ImportOps.Api.Models;

public sealed class UploadDocumentForm
{
    public IFormFile File { get; init; } = null!;

    public long? RequestId { get; init; }
    public long? QuotationId { get; init; }
    public long? OperationId { get; init; }

    public int DocumentTypeId { get; init; }

    public DateTime? DocumentDate { get; init; }
    public DateTime? ExpirationDate { get; init; }

    public string? Notes { get; init; }
    public string Visibility { get; init; } = "BOTH";
}

public sealed class ReplaceDocumentForm
{
    public IFormFile File { get; init; } = null!;
    public string? Notes { get; init; }
}
