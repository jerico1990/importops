namespace ImportOps.Application.Quotations;

public interface IQuotationProformaService
{
    Task<QuotationProformaDto?> GetAsync(long quotationId, CancellationToken ct);
    Task<(byte[] Content, string FileName)?> GeneratePdfAsync(long quotationId, CancellationToken ct);
    Task<QuotationProformaDto?> UpdateAsync(long quotationId, UpdateQuotationProformaRequest request, CancellationToken ct);
    Task<IReadOnlyCollection<QuotationConceptDto>> GetConceptsAsync(CancellationToken ct);
    Task<QuotationSupplierDto?> AddSupplierAsync(long quotationId, CreateQuotationSupplierRequest request, CancellationToken ct);
    Task<bool> DeleteSupplierAsync(long quotationId, long quotationSupplierId, CancellationToken ct);
    Task<QuotationCargoItemDto?> AddCargoAsync(long quotationId, CreateQuotationCargoItemRequest request, CancellationToken ct);
    Task<bool> DeleteCargoAsync(long quotationId, long cargoItemId, CancellationToken ct);
    Task<QuotationCustomsTaxDto?> AddTaxAsync(long quotationId, CreateQuotationCustomsTaxRequest request, CancellationToken ct);
    Task<QuotationProformaDto?> UpdateAdValoremAsync(long quotationId, UpdateAdValoremRequest request, CancellationToken ct);
    Task<bool> DeleteTaxAsync(long quotationId, long taxId, CancellationToken ct);
}
