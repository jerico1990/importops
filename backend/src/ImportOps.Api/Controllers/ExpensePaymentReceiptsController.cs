using ImportOps.Api.Security;
using ImportOps.Application.Common;
using ImportOps.Application.Documents;
using ImportOps.Application.Security;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Api.Controllers;

[ApiController]
[Route("api/finance/operations/{operationId:long}/expenses/{expenseId:long}/receipts")]
[RequirePermission(Permissions.FinanceManage)]
public sealed class ExpensePaymentReceiptsController(
    ImportOpsDbContext db, IFileStorage storage, ICurrentCompany company, ICurrentUser user) : ControllerBase
{
    private const long MaxPdfSize = 15L * 1024 * 1024;

    private IActionResult? DenyClient() => user.IsClientUser ? Forbid() : null;

    private async Task<OperationExpense?> FindExpense(long operationId, long expenseId, CancellationToken ct)
    {
        // Tenant and operation scope are checked on EVERY operation, including downloads.
        if (!await db.ImportOperations.AsNoTracking().AnyAsync(x => x.CompanyId == company.CompanyId && x.OperationId == operationId, ct)) return null;
        return await db.OperationExpenses.AsNoTracking().SingleOrDefaultAsync(x =>
            x.CompanyId == company.CompanyId && x.OperationId == operationId && x.OperationExpenseId == expenseId, ct);
    }

    private static object Dto(ExpensePaymentReceipt x) => new {
        x.ExpensePaymentReceiptId, x.OperationExpenseId, x.PaymentDate, x.Amount,
        x.CurrencyCode, x.ReferenceNo, x.OriginalFileName, x.FileSizeBytes,
        x.UploadedAt, x.UploadedBy, x.IncludeInClientLiquidation, x.IsVoid, x.VoidedAt, x.VoidReason
    };

    [HttpGet]
    public async Task<IActionResult> List(long operationId, long expenseId, CancellationToken ct)
    {
        if (DenyClient() is { } denied) return denied;
        if (await FindExpense(operationId, expenseId, ct) is null) return NotFound();
        var rows = await db.ExpensePaymentReceipts.AsNoTracking()
            .Where(x => x.CompanyId == company.CompanyId && x.OperationId == operationId && x.OperationExpenseId == expenseId)
            .OrderBy(x => x.PaymentDate).ThenBy(x => x.ExpensePaymentReceiptId).ToListAsync(ct);
        return Ok(rows.Select(Dto));
    }

    public sealed class UploadReceiptForm
    {
        public IFormFile? File { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? ReferenceNo { get; set; }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxPdfSize + 1024 * 1024)]
    public async Task<IActionResult> Upload(long operationId, long expenseId,
        [FromForm] UploadReceiptForm form, CancellationToken ct)
    {
        var file = form.File;
        var paymentDate = form.PaymentDate;
        var amount = form.Amount;
        var referenceNo = form.ReferenceNo;
        if (DenyClient() is { } denied) return denied;
        var expense = await FindExpense(operationId, expenseId, ct);
        if (expense is null) return NotFound();
        if (file is null || file.Length < 5 || file.Length > MaxPdfSize ||
            !string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Adjunta un PDF válido de hasta 15 MB.");
        if (paymentDate == default || paymentDate.Date > DateTime.UtcNow.Date)
            return BadRequest("La fecha de pago es obligatoria y no puede ser futura.");
        if (amount <= 0 || decimal.Round(amount, 2) != amount)
            return BadRequest("El importe debe ser positivo y tener hasta dos decimales.");
        if (referenceNo?.Length > 150) return BadRequest("La referencia es demasiado larga.");
        await using var stream = file.OpenReadStream();
        var signature = new byte[5];
        if (await stream.ReadAsync(signature.AsMemory(0, 5), ct) != 5 ||
            !signature.SequenceEqual(new byte[] { 37, 80, 68, 70, 45 }))
            return BadRequest("El archivo no contiene una cabecera PDF válida.");
        if (!stream.CanSeek) return BadRequest("No se pudo procesar el archivo PDF.");
        stream.Position = 0;
        // Serialización transaccional para evitar dos cargas simultáneas que superen el importe.
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var documented = await db.ExpensePaymentReceipts
            .Where(x => x.CompanyId == company.CompanyId && x.OperationExpenseId == expenseId && !x.IsVoid)
            .SumAsync(x => (decimal?)x.Amount, ct) ?? 0m;
        if (documented + amount > expense.Amount)
            return BadRequest("La suma documentada supera el importe del gasto.");
        var saved = await storage.SaveAsync(stream, Path.GetFileName(file.FileName), "application/pdf", ct);
        var receipt = new ExpensePaymentReceipt {
            CompanyId = company.CompanyId, OperationId = operationId, OperationExpenseId = expenseId,
            PaymentDate = paymentDate.Date, Amount = amount, CurrencyCode = expense.CurrencyCode,
            ReferenceNo = string.IsNullOrWhiteSpace(referenceNo) ? null : referenceNo.Trim(),
            OriginalFileName = Path.GetFileName(file.FileName), StorageKey = saved.StorageKey,
            StorageProvider = saved.Provider, Sha256Hash = saved.Sha256, FileSizeBytes = saved.SizeBytes,
            IncludeInClientLiquidation = false, // Explicit approval required before sharing with client.
            UploadedAt = DateTime.UtcNow, UploadedBy = user.UserId
        };
        try {
            db.ExpensePaymentReceipts.Add(receipt);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        } catch {
            await tx.RollbackAsync(CancellationToken.None);
            try { await storage.DeleteAsync(saved.StorageKey, CancellationToken.None); } catch { /* registrar en monitoreo de almacenamiento */ }
            throw;
        }
        return Ok(Dto(receipt));
    }

    public sealed record ClientAnnexRequest(bool Include);

    [HttpPut("{receiptId:long}/client-annex")]
    public async Task<IActionResult> SetClientAnnex(long operationId, long expenseId, long receiptId,
        [FromBody] ClientAnnexRequest request, CancellationToken ct)
    {
        if (DenyClient() is { } denied) return denied;
        if (await FindExpense(operationId, expenseId, ct) is null) return NotFound();
        // Issued liquidations must not change their annex set silently.
        if (await db.IssuedLiquidations.AnyAsync(x => x.CompanyId == company.CompanyId && x.OperationId == operationId, ct))
            return Conflict("La liquidación ya fue emitida; sus anexos no se pueden modificar.");
        var row = await db.ExpensePaymentReceipts.SingleOrDefaultAsync(x =>
            x.CompanyId == company.CompanyId && x.OperationId == operationId &&
            x.OperationExpenseId == expenseId && x.ExpensePaymentReceiptId == receiptId && !x.IsVoid, ct);
        if (row is null) return NotFound();
        row.IncludeInClientLiquidation = request.Include;
        await db.SaveChangesAsync(ct);
        return Ok(Dto(row));
    }

    [HttpGet("{receiptId:long}/download")]
    public async Task<IActionResult> Download(long operationId, long expenseId, long receiptId, CancellationToken ct)
    {
        if (DenyClient() is { } denied) return denied;
        if (await FindExpense(operationId, expenseId, ct) is null) return NotFound();
        var row = await db.ExpensePaymentReceipts.AsNoTracking().SingleOrDefaultAsync(x =>
            x.CompanyId == company.CompanyId && x.OperationId == operationId && x.OperationExpenseId == expenseId &&
            x.ExpensePaymentReceiptId == receiptId && !x.IsVoid, ct);
        if (row is null) return NotFound();
        return File(await storage.OpenReadAsync(row.StorageKey, ct), "application/pdf", row.OriginalFileName);
    }

    public sealed record VoidReceiptRequest(string Reason);

    [HttpPost("{receiptId:long}/void")]
    public async Task<IActionResult> Void(long operationId, long expenseId, long receiptId,
        [FromBody] VoidReceiptRequest request, CancellationToken ct)
    {
        if (DenyClient() is { } denied) return denied;
        if (await FindExpense(operationId, expenseId, ct) is null) return NotFound();
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 5)
            return BadRequest("Indica un motivo de al menos cinco caracteres.");
        var row = await db.ExpensePaymentReceipts.SingleOrDefaultAsync(x =>
            x.CompanyId == company.CompanyId && x.OperationId == operationId && x.OperationExpenseId == expenseId &&
            x.ExpensePaymentReceiptId == receiptId, ct);
        if (row is null) return NotFound();
        if (row.IsVoid) return Conflict("La constancia ya está anulada.");
        if (row.IncludeInClientLiquidation && await db.IssuedLiquidations.AnyAsync(x => x.CompanyId == company.CompanyId && x.OperationId == operationId, ct))
            return Conflict("No se puede anular un anexo de una liquidación ya emitida.");
        row.IsVoid = true; row.VoidedAt = DateTime.UtcNow; row.VoidedBy = user.UserId;
        row.VoidReason = request.Reason.Trim()[..Math.Min(500, request.Reason.Trim().Length)];
        await db.SaveChangesAsync(ct);
        return Ok(Dto(row));
    }
}
