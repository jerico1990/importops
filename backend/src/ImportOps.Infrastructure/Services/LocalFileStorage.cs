using System.Security.Cryptography;
using ImportOps.Application.Documents;
using Microsoft.Extensions.Configuration;

namespace ImportOps.Infrastructure.Services;

public sealed class LocalFileStorage(IConfiguration configuration) : IFileStorage
{
    private readonly string _root = Path.GetFullPath(
        configuration["Storage:LocalRoot"] ?? Path.Combine(AppContext.BaseDirectory, "storage"));

    public async Task<StoredFileResult> SaveAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        CancellationToken ct)
    {
        Directory.CreateDirectory(_root);

        var safeName = Path.GetFileName(originalFileName);
        var ext = Path.GetExtension(safeName).ToLowerInvariant();
        var relative = Path.Combine(
            DateTime.UtcNow.ToString("yyyy"),
            DateTime.UtcNow.ToString("MM"),
            $"{Guid.NewGuid():N}{ext}");

        var full = Path.GetFullPath(Path.Combine(_root, relative));
        if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Ruta de almacenamiento inválida.");

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);

        await using var target = File.Create(full);
        using var sha = SHA256.Create();
        await using var crypto = new CryptoStream(target, sha, CryptoStreamMode.Write);

        await stream.CopyToAsync(crypto, ct);
        await crypto.FlushFinalBlockAsync(ct);

        var hash = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
        var size = new FileInfo(full).Length;

        return new("LOCAL", relative.Replace('\\','/'), Path.GetFileName(relative), size, hash);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        var full = Path.GetFullPath(Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(_root, StringComparison.OrdinalIgnoreCase))
            throw new FileNotFoundException("Archivo no encontrado.");

        Stream stream = File.OpenRead(full);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var full = Path.GetFullPath(Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (full.StartsWith(_root, StringComparison.OrdinalIgnoreCase) && File.Exists(full))
            File.Delete(full);
        return Task.CompletedTask;
    }
}
