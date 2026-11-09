
using System.Net.Security;
using System.Security.Cryptography;
using FluentFTP;
using ImportOps.Application.Documents;
using Microsoft.Extensions.Configuration;

namespace ImportOps.Infrastructure.Services;

/// <summary>
/// Almacenamiento mediante FTPS explícito (AUTH TLS).
/// Las claves nuevas utilizan el prefijo ftps/.
/// Los documentos históricos permanecen en el almacenamiento LOCAL.
/// </summary>
public sealed class FtpsFileStorage(IConfiguration configuration) : IFileStorage
{
    private readonly string _host =
        configuration["Storage:Ftps:Host"]
        ?? throw new InvalidOperationException("Storage:Ftps:Host requerido.");

    private readonly int _port =
        int.TryParse(configuration["Storage:Ftps:Port"], out var p) ? p : 21;

    private readonly string _user =
        configuration["Storage:Ftps:Username"]
        ?? throw new InvalidOperationException("Storage:Ftps:Username requerido.");

    private readonly string _password =
        configuration["Storage:Ftps:Password"]
        ?? throw new InvalidOperationException("Storage:Ftps:Password requerido.");

    private readonly string _root =
        (configuration["Storage:Ftps:RemoteRoot"] ?? "/importops").Trim('/');

    private readonly bool _allowCertificateNameMismatch =
        configuration.GetValue<bool>(
            "Storage:Ftps:AllowCertificateNameMismatch");

    private AsyncFtpClient CreateClient()
    {
        var client = new AsyncFtpClient(
            _host,
            _user,
            _password,
            _port);

        client.Config.EncryptionMode = FtpEncryptionMode.Explicit;
        client.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
        client.Config.ValidateAnyCertificate = false;

        client.ValidateCertificate += (control, e) =>
        {
            e.Accept =
                e.PolicyErrors == SslPolicyErrors.None ||
                (
                    _allowCertificateNameMismatch &&
                    e.PolicyErrors ==
                        SslPolicyErrors.RemoteCertificateNameMismatch
                );
        };

        return client;
    }

    private string RemotePath(string key)
    {
        if (!key.StartsWith("ftps/", StringComparison.Ordinal) ||
            key.Split('/').Any(x => x is ".." or "."))
        {
            throw new InvalidOperationException("Clave FTPS inválida.");
        }

        var relative = key[5..];

        return string.IsNullOrEmpty(_root)
            ? "/" + relative
            : "/" + _root + "/" + relative;
    }

    public async Task<StoredFileResult> SaveAsync(
        Stream stream,
        string originalFileName,
        string contentType,
        CancellationToken ct)
    {
        var ext = Path.GetExtension(
            Path.GetFileName(originalFileName)).ToLowerInvariant();

        var key =
            $"ftps/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";

        var remotePath = RemotePath(key);
        var tempFile = Path.GetTempFileName();

        try
        {
            await using (var file = File.Create(tempFile))
            {
                await stream.CopyToAsync(file, ct);
            }

            ct.ThrowIfCancellationRequested();

            string hash;

            await using (var hashStream = File.OpenRead(tempFile))
            {
                hash = Convert.ToHexString(
                    await SHA256.HashDataAsync(hashStream, ct))
                    .ToLowerInvariant();
            }

            var size = new FileInfo(tempFile).Length;

            using var client = CreateClient();

            await client.Connect(ct);

            var directory = remotePath[
                ..remotePath.LastIndexOf('/')];

            await client.CreateDirectory(directory, true, ct);

            var status = await client.UploadFile(
                tempFile,
                remotePath,
                FtpRemoteExists.Overwrite,
                true,
                FtpVerify.Retry,
                cancellationToken: ct);

            if (status != FtpStatus.Success)
            {
                throw new IOException(
                    $"No se pudo cargar el archivo al FTPS. Estado: {status}");
            }

            return new StoredFileResult(
                "FTPS",
                key,
                Path.GetFileName(key),
                size,
                hash);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    public async Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken ct)
    {
        var remotePath = RemotePath(storageKey);
        var target = new MemoryStream();

        try
        {
            using var client = CreateClient();

            await client.Connect(ct);

            var status = await client.DownloadStream(
                target,
                remotePath,
                0,
                null,
                ct);

            if (!status)
            {
                throw new IOException(
                    $"No se pudo descargar el archivo FTPS: {storageKey}");
            }

            target.Position = 0;

            return target;
        }
        catch
        {
            await target.DisposeAsync();
            throw;
        }
    }

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        using var client = CreateClient();

        await client.Connect(ct);

        await client.DeleteFile(RemotePath(storageKey), ct);
    }
}
