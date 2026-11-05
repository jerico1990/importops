namespace ImportOps.Application.Documents;

public sealed record StoredFileResult(
    string Provider,
    string StorageKey,
    string FileName,
    long SizeBytes,
    string Sha256);

public interface IFileStorage
{
    Task<StoredFileResult> SaveAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        CancellationToken ct);

    Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct);
    Task DeleteAsync(string storageKey, CancellationToken ct);
}
