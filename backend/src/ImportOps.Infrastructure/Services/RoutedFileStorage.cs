using ImportOps.Application.Documents;
using Microsoft.Extensions.Configuration;

namespace ImportOps.Infrastructure.Services;

/// <summary>New uploads use configured provider; existing LOCAL keys stay readable.
/// Do not remove the original local storage directory until historical files are migrated.</summary>
public sealed class RoutedFileStorage(IConfiguration config, LocalFileStorage local, FtpsFileStorage? ftps = null) : IFileStorage
{
    private readonly bool _useFtps = string.Equals(config["Storage:Provider"], "FTPS", StringComparison.OrdinalIgnoreCase);
    public Task<StoredFileResult> SaveAsync(Stream stream, string originalFileName, string contentType, CancellationToken ct) =>
        _useFtps ? (ftps ?? throw new InvalidOperationException("FTPS no configurado.")).SaveAsync(stream, originalFileName, contentType, ct)
                 : local.SaveAsync(stream, originalFileName, contentType, ct);
    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct) =>
        storageKey.StartsWith("ftps/", StringComparison.Ordinal) ? (ftps ?? throw new InvalidOperationException("FTPS no configurado.")).OpenReadAsync(storageKey, ct)
                                                                : local.OpenReadAsync(storageKey, ct);
    public Task DeleteAsync(string storageKey, CancellationToken ct) =>
        storageKey.StartsWith("ftps/", StringComparison.Ordinal) ? (ftps ?? throw new InvalidOperationException("FTPS no configurado.")).DeleteAsync(storageKey, ct)
                                                                : local.DeleteAsync(storageKey, ct);
}
