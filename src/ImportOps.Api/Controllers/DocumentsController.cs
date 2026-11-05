using ImportOps.Api.Security;
using ImportOps.Api.Models;
using ImportOps.Application.Documents;
using ImportOps.Application.Security;
using Microsoft.AspNetCore.Mvc;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/documents")]
[RequirePermission(Permissions.DocumentsView)]
public sealed class DocumentsController(IDocumentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DocumentQuery query,CancellationToken ct)=>Ok(await service.GetAsync(query,ct));

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id,CancellationToken ct)
    {
        var r=await service.GetByIdAsync(id,ct);return r is null?NotFound():Ok(r);
    }

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id,CancellationToken ct)
    {
        var r=await service.DownloadAsync(id,ct);return r is null?NotFound():File(r.Value.Stream,r.Value.ContentType,r.Value.FileName);
    }

    [HttpGet("{id:long}/observations")]
    public async Task<IActionResult> Observations(long id,CancellationToken ct)
        => Ok(await service.GetObservationsAsync(id,ct));

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [RequirePermission(Permissions.DocumentsUpload)]
    public async Task<IActionResult> Upload(
        [FromForm] UploadDocumentForm form,
        CancellationToken ct)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest(new
            {
                code = "VALIDATION_ERROR",
                message = "El archivo es obligatorio."
            });

        await using var stream = form.File.OpenReadStream();

        var result = await service.UploadAsync(
            new()
            {
                RequestId = form.RequestId,
                QuotationId = form.QuotationId,
                OperationId = form.OperationId,
                DocumentTypeId = form.DocumentTypeId,
                DocumentDate = form.DocumentDate,
                ExpirationDate = form.ExpirationDate,
                Notes = form.Notes,
                Visibility = form.Visibility
            },
            stream,
            form.File.FileName,
            form.File.ContentType,
            form.File.Length,
            ct);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.DocumentId },
            result);
    }

    [HttpPost("{id:long}/replace")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    [RequirePermission(Permissions.DocumentsUpload)]
    public async Task<IActionResult> Replace(
        long id,
        [FromForm] ReplaceDocumentForm form,
        CancellationToken ct)
    {
        if (form.File is null || form.File.Length == 0)
            return BadRequest(new
            {
                code = "VALIDATION_ERROR",
                message = "El archivo es obligatorio."
            });

        await using var stream = form.File.OpenReadStream();

        var result = await service.ReplaceAsync(
            id,
            stream,
            form.File.FileName,
            form.File.ContentType,
            form.File.Length,
            form.Notes,
            ct);

        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:long}/validate")]
    [RequirePermission(Permissions.DocumentsManage)]
    public async Task<IActionResult> Validate(long id,[FromBody] ValidateDocumentRequest request,CancellationToken ct){var r=await service.ValidateAsync(id,request,ct);return r is null?NotFound():Ok(r);}

    [HttpPost("{id:long}/observe")]
    [RequirePermission(Permissions.DocumentsManage)]
    public async Task<IActionResult> Observe(long id,[FromBody] ObserveDocumentRequest request,CancellationToken ct){var r=await service.ObserveAsync(id,request,ct);return r is null?NotFound():Ok(r);}

    [HttpPost("{id:long}/observations/{observationId:long}/resolve")]
    [RequirePermission(Permissions.DocumentsManage)]
    public async Task<IActionResult> Resolve(long id,long observationId,[FromBody] ResolveDocumentObservationRequest request,CancellationToken ct){var r=await service.ResolveObservationAsync(id,observationId,request,ct);return r is null?NotFound():Ok(r);}

    [HttpPost("{id:long}/void")]
    [RequirePermission(Permissions.DocumentsManage)]
    public async Task<IActionResult> Void(long id,[FromBody] VoidDocumentRequest request,CancellationToken ct){var r=await service.VoidAsync(id,request,ct);return r is null?NotFound():Ok(r);}

    [HttpGet("operation/{operationId:long}/requirements")]
    public async Task<IActionResult> Requirements(long operationId,CancellationToken ct)=>Ok(await service.GetOperationRequirementsAsync(operationId,ct));

    [HttpPost("operation/{operationId:long}/refresh-status")]
    [RequirePermission(Permissions.DocumentsManage)]
    public async Task<IActionResult> Refresh(long operationId,CancellationToken ct)=>Ok(await service.RefreshOperationDocumentaryStatusAsync(operationId,ct));
}
