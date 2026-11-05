using System.Globalization;
using ImportOps.Application.Documents;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System.Text.Json;
using ImportOps.Application.Common;
using ImportOps.Application.Finance;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class LiquidationService(ImportOpsDbContext db, ICurrentCompany company, ICurrentUser user, IFileStorage storage) : ILiquidationService
{
    private async Task<ImportOperation> AuthorizedOperation(long id, bool internalOnly, CancellationToken ct)
    {
        if (internalOnly && user.IsClientUser) throw new BusinessException(ErrorCodes.Forbidden, "Solo personal interno puede emitir liquidaciones.", 403);
        var op = await db.ImportOperations.AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == id && x.CompanyId == company.CompanyId, ct)
            ?? throw new BusinessException(ErrorCodes.NotFound, "Operación no encontrada.", 404);
        if (user.IsClientUser && op.ClientId != user.ClientId) throw new BusinessException(ErrorCodes.NotFound, "Operación no encontrada.", 404);
        return op;
    }

    // El plazo se fija por operación. Si aún no se configuró, se hereda del cliente.
    public async Task<FinancingTermsDto> TermsAsync(long operationId, CancellationToken ct)
    {
        var op = await AuthorizedOperation(operationId, false, ct);
        var issued = await db.IssuedLiquidations.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == company.CompanyId && x.OperationId == operationId, ct);
        if (issued != null) return new(operationId, issued.FinancingDays, "ISSUED", true);
        var clientDays = await db.Clients.AsNoTracking().Where(x => x.ClientId == op.ClientId && x.CompanyId == company.CompanyId).Select(x => x.CreditDays).SingleAsync(ct);
        return new(operationId, op.FinancingDays ?? clientDays ?? 0, op.FinancingDays.HasValue ? "OPERATION" : "CLIENT", false);
    }

    public async Task<FinancingTermsDto> SetTermsAsync(long operationId, int days, CancellationToken ct)
    {
        await AuthorizedOperation(operationId, true, ct);
        if (days < 0 || days > 365) throw new BusinessException("INVALID_FINANCING_DAYS", "El plazo debe estar entre 0 y 365 días calendario.", 400);
        if (await db.IssuedLiquidations.AnyAsync(x => x.CompanyId == company.CompanyId && x.OperationId == operationId, ct))
            throw new BusinessException("LIQUIDATION_ALREADY_ISSUED", "No se puede modificar el plazo después de emitir la liquidación.", 409);
        var op = await db.ImportOperations.SingleAsync(x => x.CompanyId == company.CompanyId && x.OperationId == operationId, ct);
        op.FinancingDays = days;
        op.ModifiedAt = DateTime.UtcNow;
        op.ModifiedBy = user.UserId;
        await db.SaveChangesAsync(ct);
        return new(operationId, days, "OPERATION", false);
    }

    private static LiquidationDeliveryDto ToDelivery(IssuedLiquidation x) =>
        new(x.OperationId, x.Number, x.FinancingDays, x.IssuedAt, x.SentAt, x.DueAt, x.SentVia, x.SentTo,
            x.SentAt is null ? "ISSUED_NOT_SENT" : DateTime.UtcNow.Date > x.DueAt!.Value.Date ? "OVERDUE_BY_DATE" : "SENT");

    public async Task<LiquidationDeliveryDto?> DeliveryAsync(long operationId, CancellationToken ct)
    {
        await AuthorizedOperation(operationId, false, ct);
        var row = await db.IssuedLiquidations.AsNoTracking().SingleOrDefaultAsync(x => x.CompanyId == company.CompanyId && x.OperationId == operationId, ct);
        return row is null ? null : ToDelivery(row);
    }

    public async Task<LiquidationDeliveryDto> SendAsync(long operationId, SendLiquidationRequest request, CancellationToken ct)
    {
        await AuthorizedOperation(operationId, true, ct);
        if (request is null || string.IsNullOrWhiteSpace(request.SentVia) || request.SentVia.Length > 40 || request.SentTo?.Length > 250)
            throw new BusinessException("INVALID_DELIVERY", "Indique el medio de envío (máximo 40 caracteres) y destinatario (máximo 250).", 400);
        var row = await db.IssuedLiquidations.SingleOrDefaultAsync(x => x.CompanyId == company.CompanyId && x.OperationId == operationId, ct)
            ?? throw new BusinessException("LIQUIDATION_NOT_ISSUED", "Primero debe emitirse la liquidación.", 409);
        if (row.SentAt.HasValue) throw new BusinessException("LIQUIDATION_ALREADY_SENT", "El envío ya fue registrado; no se sobrescribe.", 409);
        var sentAt = request.SentAt ?? DateTime.UtcNow;
        if (sentAt.Kind == DateTimeKind.Unspecified) sentAt = DateTime.SpecifyKind(sentAt, DateTimeKind.Utc);
        sentAt = sentAt.ToUniversalTime();
        if (sentAt > DateTime.UtcNow.AddMinutes(5) || sentAt < row.IssuedAt.AddMinutes(-1))
            throw new BusinessException("INVALID_SENT_DATE", "La fecha de envío no puede ser anterior a la emisión ni futura.", 400);
        row.SentAt = sentAt;
        row.SentBy = user.UserId;
        row.SentVia = request.SentVia.Trim();
        row.SentTo = request.SentTo?.Trim();
        row.DueAt = sentAt.Date.AddDays(row.FinancingDays);
        await db.SaveChangesAsync(ct);
        return ToDelivery(row);
    }

    public async Task<LiquidationPreviewDto> PreviewAsync(long operationId, CancellationToken ct)
    {
        var op = await AuthorizedOperation(operationId, true, ct);
        var co = await db.Companies.AsNoTracking().SingleAsync(x => x.CompanyId == company.CompanyId, ct);
        // La moneda de la liquidación pertenece a la operación, no a la empresa.
        var currency = (op.DeclaredCurrencyCode ?? op.QuotationCurrencySnapshot ?? co.BaseCurrencyCode).Trim().ToUpperInvariant();
        var companyCurrency = co.BaseCurrencyCode.Trim().ToUpperInvariant();
        var expenses = await db.OperationExpenses.AsNoTracking().Where(x => x.CompanyId == company.CompanyId && x.OperationId == operationId && !x.IsDeleted && x.ExpenseStatus != "CANCELLED")
            .OrderBy(x => x.ExpenseDate).ToListAsync(ct);
        var services = await db.OperationServices.AsNoTracking().Where(x => x.CompanyId == company.CompanyId && x.OperationId == operationId && x.ServiceStatus != "CANCELLED").ToListAsync(ct);
        var funds = await db.ClientFunds.AsNoTracking().Where(x => x.CompanyId == company.CompanyId && x.OperationId == operationId && !x.IsDeleted && x.FundStatus == "RECEIVED").ToListAsync(ct);
        var collections = await db.ClientCollections.AsNoTracking().Where(x => x.CompanyId == company.CompanyId && x.OperationId == operationId && !x.IsVoid && (x.CollectionStatus == "RECEIVED" || x.CollectionStatus == "APPLIED")).ToListAsync(ct);
        var refunds = await db.ClientRefunds.AsNoTracking().Where(x => x.CompanyId == company.CompanyId && x.OperationId == operationId).ToListAsync(ct);
        var adjustments = await db.OperationAdjustments.AsNoTracking().Where(x => x.OperationId == operationId && !x.IsVoid).ToListAsync(ct);
        var expenseIds = expenses.Select(x => x.OperationExpenseId).ToList();
        var payments = await db.ExpensePayments.AsNoTracking().Where(x => expenseIds.Contains(x.OperationExpenseId) && !x.IsVoid).ToListAsync(ct);
        var warnings = new List<string>();
        // R&B registra gastos ya realizados; no exigir un segundo movimiento de pago.
        // Los gastos pagados directamente por el importador se informan sin generar deuda para R&B.
        if (collections.Any(x => x.CollectionType != "SERVICE")) warnings.Add("Existen cobros FUND/OTHER; revisar su naturaleza para evitar doble contabilización.");
        if (adjustments.Count != 0) warnings.Add("Existen ajustes financieros que deben conciliarse antes de emitir.");
        if (refunds.Any(x => x.RefundStatus != "PAID" && x.RefundStatus != "COMPLETED" && x.RefundStatus != "CANCELLED")) warnings.Add("Existen devoluciones sin confirmar.");
        // La liquidación se presenta por moneda original: no mezclar ni convertir silenciosamente PEN y USD.
        // Las constancias privadas y los pagos a proveedores NO son líneas de liquidación.
        var taxes = await db.CustomsTaxFinanceItems.AsNoTracking()
            .Where(x => x.CompanyId == company.CompanyId && x.OperationId == operationId && !x.IsVoid).ToListAsync(ct);
        var taxPayments = await db.CustomsTaxFinancePayments.AsNoTracking()
            .Where(x => x.CompanyId == company.CompanyId && x.OperationId == operationId && !x.IsVoid).ToListAsync(ct);
        if (expenses.Any(e => e.IsRebillable && string.IsNullOrEmpty(e.PaidBy)))
            warnings.Add("Existen gastos históricos cuyo responsable de desembolso debe revisarse.");
        if (taxes.Any(t => t.PayerType == "UNREVIEWED"))
            warnings.Add("Existen tributos históricos cuyo responsable de desembolso debe revisarse.");
        if (expenses.Any(x => x.CurrencyCode != "PEN" && x.CurrencyCode != "USD") ||
            services.Any(x => x.CurrencyCode != "PEN" && x.CurrencyCode != "USD") ||
            funds.Any(x => x.CurrencyCode != "PEN" && x.CurrencyCode != "USD") ||
            collections.Any(x => x.CurrencyCode != "PEN" && x.CurrencyCode != "USD") ||
            refunds.Any(x => x.CurrencyCode != "PEN" && x.CurrencyCode != "USD"))
            warnings.Add("Existen movimientos en moneda distinta de PEN o USD; revisar antes de emitir.");
        var sections = new List<LiquidationCurrencyDto>();
        foreach (var code in new[] { "USD", "PEN" })
        {
            var expenseRows = expenses.Where(x => x.CurrencyCode == code && (x.IsRebillable || x.PaidBy == "CLIENT")).ToList();
            var serviceRows = services.Where(x => x.CurrencyCode == code).ToList();
            var lines = expenseRows.Select(x => new LiquidationLineDto("EXPENSE", x.OperationExpenseId, x.Description, x.DocumentReference, x.Amount, code, x.PaidBy ?? "UNREVIEWED"))
                .Concat(serviceRows.Select(x => new LiquidationLineDto("SERVICE", x.OperationServiceId, x.Description, null, x.Amount, code))).ToList();
            decimal taxTotal = 0m;
            decimal directTaxPaid = 0m;
            if (code == "PEN")
            {
                foreach (var tax in taxes)
                {
                    // El registro de tributo equivale a un desembolso realizado.
                    lines.Add(new LiquidationLineDto("CUSTOMS_TAX", tax.CustomsTaxItemId, tax.Description, tax.DocumentReference, tax.AmountPen, "PEN", tax.PayerType));
                    taxTotal += tax.AmountPen;
                }
                directTaxPaid = taxes.Where(t => t.PayerType == "CLIENT").Sum(t => t.AmountPen);
            }
            var expenseTotal = expenseRows.Sum(x => x.Amount);
            var serviceTotal = serviceRows.Sum(x => x.Amount);
            var advances = funds.Where(x => x.CurrencyCode == code).Sum(x => x.ReceivedAmount ?? 0m);
            var collected = collections.Where(x => x.CurrencyCode == code && x.CollectionType == "SERVICE").Sum(x => x.Amount);
            var refunded = refunds.Where(x => x.CurrencyCode == code && (x.RefundStatus == "PAID" || x.RefundStatus == "COMPLETED")).Sum(x => x.Amount);
            var due = expenseTotal + serviceTotal + taxTotal;
            var directExpenses = expenseRows.Where(x => x.PaidBy == "CLIENT").Sum(x => x.Amount);
            var received = advances + collected + directTaxPaid + directExpenses;
            sections.Add(new LiquidationCurrencyDto(code, lines, expenseTotal, serviceTotal, taxTotal, due,
                advances, collected, directTaxPaid + directExpenses, received, refunded, decimal.Round(received - refunded - due, 2)));
        }
        // Los campos agregados heredados representan solamente la moneda principal.
        // Los consumidores nuevos DEBEN usar CurrencySections para no mezclar monedas.
        var main = sections.First(x => x.CurrencyCode == (currency == "PEN" ? "PEN" : "USD"));
        warnings = warnings.Distinct().ToList();
        return new LiquidationPreviewDto(operationId, op.OperationNo, op.ClientNameSnapshot ?? "", co.LegalName,
            op.MerchandiseDescription, currency, main.Lines, main.TotalExpenses, main.TotalServices,
            main.TotalDue, main.ClientAdvances, main.ServiceCollections, main.TotalReceived,
            main.TotalRefunded, main.Balance, warnings, sections);
    }

    public async Task<LiquidationPreviewDto> IssueAsync(long operationId, CancellationToken ct)
    {
        var op = await AuthorizedOperation(operationId, true, ct);
        // Serializable transaction + unique constraints prevent two concurrent issues for the same operation/number.
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var previous = await db.IssuedLiquidations.AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == operationId && x.CompanyId == company.CompanyId, ct);
        if (previous is not null) throw new BusinessException("LIQUIDATION_ALREADY_ISSUED", "La operación ya tiene una liquidación emitida. No se sobrescribe.", 409);
        var preview = await PreviewAsync(operationId, ct);
        if (preview.Warnings.Count != 0) throw new BusinessException("LIQUIDATION_RECONCILIATION_REQUIRED", "No se puede emitir: " + string.Join(" ", preview.Warnings), 409);
        if (preview.CurrencySections?.All(x => x.Lines.Count == 0) != false) throw new BusinessException("LIQUIDATION_EMPTY", "No hay gastos o servicios para liquidar.", 409);
        var year = DateTime.UtcNow.Year;
        var last = await db.IssuedLiquidations.Where(x => x.CompanyId == company.CompanyId && x.YearNo == year)
            .MaxAsync(x => (int?)x.SequenceNo, ct) ?? 0;
        var sequence = last + 1;
        var number = $"{sequence:0000}-{year}";
        var terms = await TermsAsync(operationId, ct);
        var issued = preview with { Number = number, IssuedAt = DateTime.UtcNow };
        db.IssuedLiquidations.Add(new IssuedLiquidation { CompanyId = company.CompanyId, OperationId = operationId,
            ClientId = op.ClientId, SequenceNo = sequence, YearNo = year, Number = number,
            CurrencyCode = issued.CurrencyCode, SnapshotJson = JsonSerializer.Serialize(issued),
            IssuedAt = issued.IssuedAt!.Value, IssuedBy = user.UserId, FinancingDays = terms.FinancingDays });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return issued;
    }

    public async Task<LiquidationPreviewDto?> IssuedAsync(long operationId, CancellationToken ct)
    {
        await AuthorizedOperation(operationId, false, ct);
        var row = await db.IssuedLiquidations.AsNoTracking().SingleOrDefaultAsync(x => x.OperationId == operationId && x.CompanyId == company.CompanyId, ct);
        return row is null ? null : JsonSerializer.Deserialize<LiquidationPreviewDto>(row.SnapshotJson);
    }

    public async Task<(byte[] Content, string FileName)?> PdfAsync(long operationId, CancellationToken ct)
    {
        var p = await IssuedAsync(operationId, ct);
        if (p is null) return null;
        static string M(decimal amount) => amount.ToString("N2", CultureInfo.GetCultureInfo("en-US"));
        var lines = new List<SimpleQuotationPdf.Line> {
            new(p.CompanyName, 14, true), new($"LIQUIDACION DE IMPORTACION N. {p.Number}", 12, true, RuleAfter: true),
            new($"Fecha de emision: {p.IssuedAt:dd/MM/yyyy}  |  Operacion: {p.OperationNo}"),
            new($"Cliente: {p.ClientName}"), new($"Mercancia: {p.Merchandise}")
        };
        foreach (var section in p.CurrencySections ?? new[] { new LiquidationCurrencyDto(p.CurrencyCode, p.Lines, p.TotalExpenses, p.TotalServices, 0m, p.TotalDue, p.ClientAdvances, p.ServiceCollections, 0m, p.TotalReceived, p.TotalRefunded, p.Balance) })
        {
            lines.Add(new($"DETALLE EN {section.CurrencyCode}", 11, true, RuleAfter: true));
            foreach (var item in section.Lines)
            {
                var description = item.Description.Length > 67 ? item.Description[..67] : item.Description;
                lines.Add(new($"{description}   {section.CurrencyCode} {M(item.Amount)}  {(item.PayerType == "CLIENT" ? "[Pagado por importador]" : item.PayerType == "COMPANY" ? "[R&B]" : "[Por revisar]")}"));
                if (!string.IsNullOrWhiteSpace(item.Reference)) lines.Add(new($"   Referencia: {item.Reference}", 8));
            }
            lines.Add(new($"TOTAL {section.CurrencyCode}: {M(section.TotalDue)}", 11, true, RuleAfter: true));
            lines.Add(new($"Anticipos recibidos: {section.CurrencyCode} {M(section.ClientAdvances)}"));
            lines.Add(new($"Cobros de servicios: {section.CurrencyCode} {M(section.ServiceCollections)}"));
            lines.Add(new($"Desembolsos directos del importador (informativos): {section.CurrencyCode} {M(section.DirectClientTaxPayments)}"));
            lines.Add(new($"Devoluciones efectuadas: {section.CurrencyCode} {M(section.TotalRefunded)}"));
            lines.Add(new($"SALDO CLIENTE (+) / POR COBRAR (-): {section.CurrencyCode} {M(section.Balance)}", 10, true, RuleAfter: true));
        }
        var basePdf = SimpleQuotationPdf.Build(lines);
        // Only explicitly authorized, active annexes for expenses present in the issued snapshot.
        var expenseIds = (p.CurrencySections?.SelectMany(s => s.Lines) ?? p.Lines)
            .Where(l => l.Source.Equals("EXPENSE", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.SourceId).ToHashSet() ?? new HashSet<long>();
        var annexes = await db.ExpensePaymentReceipts.AsNoTracking()
            .Where(x => x.CompanyId == company.CompanyId && x.OperationId == operationId &&
                        !x.IsVoid && x.IncludeInClientLiquidation)
            .OrderBy(x => x.OperationExpenseId).ThenBy(x => x.ExpensePaymentReceiptId).ToListAsync(ct);
        annexes = annexes.Where(x => expenseIds.Contains(x.OperationExpenseId)).ToList();
        if (annexes.Count == 0) return (basePdf, $"Liquidacion-{p.Number}.pdf");
        using var merged = new PdfDocument();
        var sources = new List<PdfDocument>();
        try
        {
            var first = PdfReader.Open(new MemoryStream(basePdf), PdfDocumentOpenMode.Import);
            sources.Add(first);
            for (var i = 0; i < first.PageCount; i++) merged.AddPage(first.Pages[i]);
            foreach (var annex in annexes)
            {
                await using var stored = await storage.OpenReadAsync(annex.StorageKey, ct);
                using var buffer = new MemoryStream();
                await stored.CopyToAsync(buffer, ct);
                // Fail the entire download if an approved attachment cannot be merged.
                var source = PdfReader.Open(new MemoryStream(buffer.ToArray()), PdfDocumentOpenMode.Import);
                sources.Add(source);
                for (var i = 0; i < source.PageCount; i++) merged.AddPage(source.Pages[i]);
            }
            using var output = new MemoryStream();
            merged.Save(output, false);
            return (output.ToArray(), $"Liquidacion-{p.Number}-con-anexos.pdf");
        }
        finally { foreach (var source in sources) source.Dispose(); }
    }
}
