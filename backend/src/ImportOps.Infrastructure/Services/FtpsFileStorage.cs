#pragma warning disable SYSLIB0014 // FtpWebRequest is retained for compatibility with explicit FTPS on .NET 8.
using System.Net;
using System.Security.Cryptography;
using ImportOps.Application.Documents;
using Microsoft.Extensions.Configuration;

namespace ImportOps.Infrastructure.Services;

/// <summary>Explicit FTPS (AUTH TLS) over port 21. New keys are prefixed ftps/;
/// legacy unprefixed keys remain on local storage and are read by RoutedFileStorage.</summary>
public sealed class FtpsFileStorage(IConfiguration configuration) : IFileStorage
{
    private readonly string _host = configuration["Storage:Ftps:Host"] ?? throw new InvalidOperationException("Storage:Ftps:Host requerido.");
    private readonly int _port = int.TryParse(configuration["Storage:Ftps:Port"], out var p) ? p : 21;
    private readonly string _user = configuration["Storage:Ftps:Username"] ?? throw new InvalidOperationException("Storage:Ftps:Username requerido.");
    private readonly string _password = configuration["Storage:Ftps:Password"] ?? throw new InvalidOperationException("Storage:Ftps:Password requerido.");
    private readonly string _root = (configuration["Storage:Ftps:RemoteRoot"] ?? "/importops").Trim('/');

    private FtpWebRequest Request(string path, string method)
    {
        var uri = new UriBuilder("ftp", _host, _port, "/" + string.Join('/', path.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString))).Uri;
        var req = (FtpWebRequest)WebRequest.Create(uri);
        req.Method = method;
        req.Credentials = new NetworkCredential(_user, _password);
        req.EnableSsl = true; // Explicit FTPS; TLS certificate is validated by .NET.
        req.UseBinary = true;
        req.UsePassive = true;
        req.KeepAlive = false;
        req.Timeout = 30000;
        req.ReadWriteTimeout = 120000;
        return req;
    }

    private string RemotePath(string key)
    {
        if (!key.StartsWith("ftps/", StringComparison.Ordinal) || key.Split('/').Any(x => x is ".." or "."))
            throw new InvalidOperationException("Clave FTPS inválida.");
        return string.IsNullOrEmpty(_root) ? key[5..] : _root + "/" + key[5..];
    }

    private async Task EnsureDirectoriesAsync(string path)
    {
        var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 1; i < parts.Length; i++)
        {
            var folder = string.Join('/', parts.Take(i));
            try { using var response = (FtpWebResponse)await Request(folder, WebRequestMethods.Ftp.MakeDirectory).GetResponseAsync(); }
            catch (WebException ex) when (ex.Response is FtpWebResponse r && r.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
            { r.Dispose(); /* Directory already exists on most FTP servers. */ }
        }
    }

    public async Task<StoredFileResult> SaveAsync(Stream stream, string originalFileName, string contentType, CancellationToken ct)
    {
        var ext = Path.GetExtension(Path.GetFileName(originalFileName)).ToLowerInvariant();
        var key = $"ftps/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{ext}";
        var path = RemotePath(key);
        var tmp = Path.GetTempFileName();
        try
        {
            await using (var file = File.Create(tmp)) await stream.CopyToAsync(file, ct);
            ct.ThrowIfCancellationRequested();
            await using var hashStream = File.OpenRead(tmp);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(hashStream, ct)).ToLowerInvariant();
            var size = new FileInfo(tmp).Length;
            await EnsureDirectoriesAsync(path);
            var req = Request(path, WebRequestMethods.Ftp.UploadFile);
            req.ContentLength = size;
            await using (var output = await req.GetRequestStreamAsync())
            await using (var input = File.OpenRead(tmp)) await input.CopyToAsync(output, ct);
            using var response = (FtpWebResponse)await req.GetResponseAsync();
            return new StoredFileResult("FTPS", key, Path.GetFileName(key), size, hash);
        }
        finally { File.Delete(tmp); }
    }

    public async Task<Stream> OpenReadAsync(string storageKey, CancellationToken ct)
    {
        var req = Request(RemotePath(storageKey), WebRequestMethods.Ftp.DownloadFile);
        using var response = (FtpWebResponse)await req.GetResponseAsync();
        await using var source = response.GetResponseStream() ?? throw new IOException("FTPS no devolvió contenido.");
        var target = new MemoryStream();
        try { await source.CopyToAsync(target, ct); target.Position = 0; return target; }
        catch { await target.DisposeAsync(); throw; }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        using var response = (FtpWebResponse)await Request(RemotePath(storageKey), WebRequestMethods.Ftp.DeleteFile).GetResponseAsync();
    }
}
#pragma warning restore SYSLIB0014
