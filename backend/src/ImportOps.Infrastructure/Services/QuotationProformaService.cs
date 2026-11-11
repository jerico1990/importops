using ImportOps.Application.Common;
using ImportOps.Application.Quotations;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class QuotationProformaService(ImportOpsDbContext db, ICurrentCompany company, ICurrentUser user) : IQuotationProformaService
{
    public async Task<QuotationProformaDto?> GetAsync(long id, CancellationToken ct)
    {
        var q = await Owned(id, false, ct); if (q is null) return null;
        var suppliers = await db.QuotationSuppliers.AsNoTracking().Where(x=>x.QuotationId==id).OrderBy(x=>x.DisplayOrder).Select(x=>new QuotationSupplierDto(x.QuotationSupplierId,x.SupplierId,x.Supplier.LegalName,x.DisplayOrder,x.Notes)).ToListAsync(ct);
        var cargo = await db.QuotationCargoItems.AsNoTracking().Where(x=>x.QuotationId==id).OrderBy(x=>x.LineNo).Select(x=>new QuotationCargoItemDto(x.QuotationCargoItemId,x.QuotationSupplierId,x.LineNo,x.Description,x.Quantity,x.UnitOfMeasure,x.NetWeightKg,x.GrossWeightKg,x.VolumeM3,x.DeclaredValue,x.CurrencyCode,x.OriginCountryCode,x.Notes)).ToListAsync(ct);
        var taxes = await db.QuotationCustomsTaxes.AsNoTracking().Where(x=>x.QuotationId==id).OrderBy(x=>x.DisplayOrder).Select(x=>new QuotationCustomsTaxDto(x.QuotationCustomsTaxId,x.CustomsTaxConceptId,x.CustomsTaxConcept != null ? x.CustomsTaxConcept.Code : x.TaxCode,x.CustomsTaxConcept != null ? x.CustomsTaxConcept.Name : x.Description,x.Amount,x.CurrencyCode,x.DisplayOrder,x.Notes,x.RateApplied,x.CalculationBase,x.IsCalculated,x.CustomsTaxConcept != null ? x.CustomsTaxConcept.CalculationMode : "MANUAL")).ToListAsync(ct);
        var banks = await db.CompanyBankAccounts.AsNoTracking().Where(x=>x.CompanyId==company.CompanyId && x.IsActive && x.ShowOnQuotation).OrderBy(x=>x.DisplayOrder).Select(x=>new CompanyBankAccountDto(x.CompanyBankAccountId,x.BankName,x.CurrencyCode,x.AccountNumber,x.Cci,x.AccountHolder,x.DisplayOrder)).ToListAsync(ct);
        return new(q.ReferenceFile,q.ContentDescription,q.CustomsRegimeId,q.CustomsRegimeCatalog != null ? q.CustomsRegimeCatalog.Code : null,q.CustomsRegimeCatalog != null ? q.CustomsRegimeCatalog.Name : q.CustomsRegime,q.CustomsOfficeId,q.CustomsOfficeCatalog != null ? q.CustomsOfficeCatalog.Code : null,q.CustomsOfficeCatalog != null ? q.CustomsOfficeCatalog.Name : q.CustomsOffice,q.EstimatedArrivalDate,q.ExchangeRate,q.ExwAmount??0,q.FreightAmount??0,q.InsuranceAmount??0,q.CustomsValue??0,q.NetWeightKg,q.PackageCount,q.VolumeM3,q.CustomsDutiesAmount??0,q.ExpenseAmount??0,q.ExpenseTaxAmount??0,q.TotalAmount,q.BankInstructions,q.PublicNotes,suppliers,cargo,taxes,banks);
    }


    public async Task<(byte[] Content, string FileName)?> GeneratePdfAsync(long id, CancellationToken ct)
    {
        var q = await db.Quotations.AsNoTracking().Include(x=>x.Client).SingleOrDefaultAsync(x=>x.QuotationId==id&&x.CompanyId==company.CompanyId&&(!user.IsClientUser||(x.ClientId==user.ClientId&&x.SentAt!=null)),ct);
        if(q is null) return null;
        var p = await GetAsync(id,ct); if(p is null) return null;
        var co = await db.Companies.AsNoTracking().SingleAsync(x=>x.CompanyId==company.CompanyId,ct);
        var items = await db.QuotationItems.AsNoTracking().Where(x=>x.QuotationId==id).OrderBy(x=>x.DisplayOrder??x.LineNo).ThenBy(x=>x.LineNo).ToListAsync(ct);
        var concepts = await db.QuotationConcepts.AsNoTracking().Where(x=>x.CompanyId==company.CompanyId).ToDictionaryAsync(x=>x.QuotationConceptId,ct);
        var data = new CommercialProformaPdf.Data(
            "R&B Freight Cargo S.A.C. - ImportOps",
            co.AddressLine,
            co.Phone,
            co.Email,
            q.Client.LegalName, q.QuotationNo, q.IssueDate, p.ReferenceFile,
            p.ContentDescription, p.CustomsRegimeName, p.CustomsOfficeName,
            p.EstimatedArrivalDate, p.ExchangeRate, p.ExwAmount, p.FreightAmount,
            p.InsuranceAmount, p.CustomsValue, p.NetWeightKg, p.PackageCount,
            p.VolumeM3, p.Suppliers.Select(x => x.SupplierName).ToList(),
            p.CargoItems.Select(x => x.Description).ToList(),
            p.CustomsTaxes.Select(x => new CommercialProformaPdf.AmountLine(x.Description, x.Amount, false)).ToList(),
            items.Select(x => new CommercialProformaPdf.ExpenseLine(
                x.Description,
                x.Quantity * x.UnitPrice - x.DiscountAmount,
                x.IsTaxable != true,
                x.QuotationConceptId.HasValue && concepts.TryGetValue(x.QuotationConceptId.Value, out var c)
                    ? c.GroupName : "GASTOS Y SERVICIOS")).ToList(),
            p.BankAccounts.Select(x => new CommercialProformaPdf.BankLine(x.BankName, x.CurrencyCode, x.AccountNumber, x.AccountHolder)).ToList(),
            p.BankInstructions, p.PublicNotes, p.CustomsDutiesAmount,
            p.ExpenseAmount, p.ExpenseTaxAmount, p.TotalProforma,
            q.CurrencyCode, q.Terms);
        return (CommercialProformaPdf.Build(data), $"{q.QuotationNo}-V{q.VersionNo}.pdf");
    }

    public async Task<QuotationProformaDto?> UpdateAsync(long id, UpdateQuotationProformaRequest r, CancellationToken ct)
    {
        var q=await Owned(id,true,ct); if(q is null)return null; Draft(q);
        if(r.ExchangeRate is <=0) throw new ArgumentException("ExchangeRate debe ser mayor a cero.");
        if(r.ExwAmount<0||r.FreightAmount<0||r.InsuranceAmount<0) throw new ArgumentException("FOB, flete y seguro no pueden ser negativos.");
        if(r.NetWeightKg<0||r.PackageCount<0||r.VolumeM3<0) throw new ArgumentException("Peso, bultos y volumen no pueden ser negativos.");
        db.Entry(q).Property(x=>x.RowVer).OriginalValue=ConcurrencyToken.Decode(r.RowVersion);
        CustomsRegime? regime = null;
        if (r.CustomsRegimeId.HasValue)
            regime = await db.CustomsRegimes.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive && x.CustomsRegimeId == r.CustomsRegimeId.Value, ct) ?? throw new ArgumentException("Régimen aduanero inválido.");
        CustomsOfficeCatalog? office = null;
        if (r.CustomsOfficeId.HasValue)
            office = await db.CustomsOfficeCatalogs.AsNoTracking().SingleOrDefaultAsync(x => x.IsActive && x.CustomsOfficeId == r.CustomsOfficeId.Value, ct) ?? throw new ArgumentException("Aduana inválida.");
        q.ReferenceFile=Clean(r.ReferenceFile); q.ContentDescription=Clean(r.ContentDescription); q.CustomsRegimeId=regime?.CustomsRegimeId; q.CustomsOfficeId=office?.CustomsOfficeId; q.CustomsRegime=regime?.Name; q.CustomsOffice=office?.Name; q.EstimatedArrivalDate=r.EstimatedArrivalDate?.Date; q.ExchangeRate=r.ExchangeRate;
        q.ExwAmount=R(r.ExwAmount); q.FreightAmount=R(r.FreightAmount); q.InsuranceAmount=R(r.InsuranceAmount); q.CustomsValue=QuotationProformaCalculator.CustomsValue(r.ExwAmount,r.FreightAmount,r.InsuranceAmount); q.NetWeightKg=r.NetWeightKg; q.PackageCount=r.PackageCount; q.VolumeM3=r.VolumeM3; q.BankInstructions=Clean(r.BankInstructions); q.PublicNotes=Clean(r.PublicNotes); Touch(q); await db.SaveChangesAsync(ct); await Recalc(id,ct); return await GetAsync(id,ct);
    }

    public Task<IReadOnlyCollection<QuotationConceptDto>> GetConceptsAsync(CancellationToken ct) => db.QuotationConcepts.AsNoTracking().Where(x=>x.CompanyId==company.CompanyId&&x.IsActive).OrderBy(x=>x.DisplayOrder).Select(x=>new QuotationConceptDto(x.QuotationConceptId,x.Code,x.Name,x.GroupCode,x.GroupName,x.ServiceCategoryId,x.ExpenseCategoryId,x.DefaultTaxRate,x.IsTaxable,x.IsRebillableDefault,x.DisplayOrder)).ToListAsync(ct).ContinueWith<IReadOnlyCollection<QuotationConceptDto>>(x=>x.Result,ct);

    public async Task<QuotationSupplierDto?> AddSupplierAsync(long id, CreateQuotationSupplierRequest r, CancellationToken ct){var q=await Owned(id,true,ct);if(q is null)return null;Draft(q);var s=await db.Suppliers.AsNoTracking().SingleOrDefaultAsync(x=>x.SupplierId==r.SupplierId&&x.CompanyId==company.CompanyId&&x.IsActive&&!x.IsDeleted,ct)??throw new ArgumentException("Proveedor inválido.");if(await db.QuotationSuppliers.AnyAsync(x=>x.QuotationId==id&&x.SupplierId==r.SupplierId,ct))throw new InvalidOperationException("El proveedor ya está asociado a la cotización.");var e=new QuotationSupplier{QuotationId=id,SupplierId=r.SupplierId,DisplayOrder=r.DisplayOrder,Notes=Clean(r.Notes)};db.QuotationSuppliers.Add(e);Touch(q);await db.SaveChangesAsync(ct);return new(e.QuotationSupplierId,e.SupplierId,s.LegalName,e.DisplayOrder,e.Notes);}
    public async Task<bool> DeleteSupplierAsync(long id,long sid,CancellationToken ct){var q=await Owned(id,true,ct);if(q is null)return false;Draft(q);if(await db.QuotationCargoItems.AnyAsync(x=>x.QuotationId==id&&x.QuotationSupplierId==sid,ct))throw new InvalidOperationException("No puede retirar un proveedor que tiene mercancías asociadas.");var e=await db.QuotationSuppliers.SingleOrDefaultAsync(x=>x.QuotationId==id&&x.QuotationSupplierId==sid,ct);if(e is null)return false;db.Remove(e);Touch(q);await db.SaveChangesAsync(ct);return true;}
    public async Task<QuotationCargoItemDto?> AddCargoAsync(long id,CreateQuotationCargoItemRequest r,CancellationToken ct){var q=await Owned(id,true,ct);if(q is null)return null;Draft(q);if(string.IsNullOrWhiteSpace(r.Description))throw new ArgumentException("La descripción de mercancía es obligatoria.");if(r.Quantity<=0||r.NetWeightKg<0||r.GrossWeightKg<0||r.VolumeM3<0||r.DeclaredValue<0)throw new ArgumentException("Valores de mercancía inválidos.");if(r.QuotationSupplierId.HasValue&&!await db.QuotationSuppliers.AnyAsync(x=>x.QuotationId==id&&x.QuotationSupplierId==r.QuotationSupplierId,ct))throw new ArgumentException("Proveedor de cotización inválido.");if(r.CurrencyCode!=null&&!await db.Currencies.AnyAsync(x=>x.CurrencyCode==r.CurrencyCode&&x.IsActive,ct))throw new ArgumentException("Moneda inválida.");if(r.OriginCountryCode!=null&&!await db.Countries.AnyAsync(x=>x.CountryCode==r.OriginCountryCode&&x.IsActive,ct))throw new ArgumentException("País de origen inválido.");var line=(await db.QuotationCargoItems.Where(x=>x.QuotationId==id).MaxAsync(x=>(int?)x.LineNo,ct)??0)+1;var e=new QuotationCargoItem{QuotationId=id,QuotationSupplierId=r.QuotationSupplierId,LineNo=line,Description=r.Description.Trim(),Quantity=r.Quantity,UnitOfMeasure=Clean(r.UnitOfMeasure),NetWeightKg=r.NetWeightKg,GrossWeightKg=r.GrossWeightKg,VolumeM3=r.VolumeM3,DeclaredValue=r.DeclaredValue,CurrencyCode=r.CurrencyCode?.Trim().ToUpperInvariant(),OriginCountryCode=r.OriginCountryCode?.Trim().ToUpperInvariant(),Notes=Clean(r.Notes)};db.Add(e);Touch(q);await db.SaveChangesAsync(ct);return new(e.QuotationCargoItemId,e.QuotationSupplierId,e.LineNo,e.Description,e.Quantity,e.UnitOfMeasure,e.NetWeightKg,e.GrossWeightKg,e.VolumeM3,e.DeclaredValue,e.CurrencyCode,e.OriginCountryCode,e.Notes);}
    public async Task<bool> DeleteCargoAsync(long id,long cid,CancellationToken ct){var q=await Owned(id,true,ct);if(q is null)return false;Draft(q);var e=await db.QuotationCargoItems.SingleOrDefaultAsync(x=>x.QuotationId==id&&x.QuotationCargoItemId==cid,ct);if(e is null)return false;db.Remove(e);Touch(q);await db.SaveChangesAsync(ct);return true;}
    public async Task<QuotationCustomsTaxDto?> AddTaxAsync(long id,CreateQuotationCustomsTaxRequest r,CancellationToken ct)
    {
        var q=await Owned(id,true,ct); if(q is null)return null; Draft(q);
        if(r.CustomsTaxConceptId<=0||r.Amount<0)throw new ArgumentException("Tributo inválido.");
        var taxConcept=await db.CustomsTaxConcepts.AsNoTracking().SingleOrDefaultAsync(x=>x.IsActive&&x.CustomsTaxConceptId==r.CustomsTaxConceptId,ct)??throw new ArgumentException("Concepto de derecho aduanero inválido.");
        if(!string.Equals(taxConcept.CalculationMode,"MANUAL",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException($"{taxConcept.Name} es calculado automáticamente y no admite ingreso manual.");
        var cur=r.CurrencyCode.Trim().ToUpperInvariant(); if(!await db.Currencies.AnyAsync(x=>x.CurrencyCode==cur&&x.IsActive,ct))throw new ArgumentException("Moneda inválida.");
        if(await db.QuotationCustomsTaxes.AnyAsync(x=>x.QuotationId==id&&x.CustomsTaxConceptId==r.CustomsTaxConceptId,ct))throw new InvalidOperationException("El tributo ya existe en la cotización.");
        var e=new QuotationCustomsTax{QuotationId=id,CustomsTaxConceptId=taxConcept.CustomsTaxConceptId,TaxCode=taxConcept.Code,Description=taxConcept.Name,Amount=R(r.Amount),CurrencyCode=cur,DisplayOrder=taxConcept.DisplayOrder,Notes=Clean(r.Notes),IsCalculated=false};
        db.Add(e); Touch(q); await db.SaveChangesAsync(ct); await Recalc(id,ct);
        return new(e.QuotationCustomsTaxId,taxConcept.CustomsTaxConceptId,taxConcept.Code,taxConcept.Name,e.Amount,e.CurrencyCode,e.DisplayOrder,e.Notes,e.RateApplied,e.CalculationBase,e.IsCalculated,taxConcept.CalculationMode);
    }

    public async Task<QuotationProformaDto?> UpdateAdValoremAsync(long id,UpdateAdValoremRequest r,CancellationToken ct)
    {
        var q=await Owned(id,true,ct); if(q is null)return null; Draft(q);
        if(r.Amount<0) throw new ArgumentException("Ad Valorem no puede ser negativo.");
        var concept=await db.CustomsTaxConcepts.SingleOrDefaultAsync(x=>x.IsActive&&x.Code=="ADVALOREM",ct) ?? throw new InvalidOperationException("No existe el concepto ADVALOREM.");
        var cur=r.CurrencyCode.Trim().ToUpperInvariant(); if(!await db.Currencies.AnyAsync(x=>x.CurrencyCode==cur&&x.IsActive,ct))throw new ArgumentException("Moneda inválida.");
        var line=await db.QuotationCustomsTaxes.SingleOrDefaultAsync(x=>x.QuotationId==id&&x.CustomsTaxConceptId==concept.CustomsTaxConceptId,ct);
        if(line is null){line=new QuotationCustomsTax{QuotationId=id,CustomsTaxConceptId=concept.CustomsTaxConceptId,TaxCode=concept.Code,Description=concept.Name,CurrencyCode=cur,DisplayOrder=concept.DisplayOrder};db.Add(line);}
        line.Amount=R(r.Amount); line.CurrencyCode=cur; line.Notes=Clean(r.Notes); line.IsCalculated=false; line.RateApplied=null; line.CalculationBase=null;
        Touch(q); await db.SaveChangesAsync(ct); await Recalc(id,ct); return await GetAsync(id,ct);
    }
    public async Task<bool> DeleteTaxAsync(long id,long tid,CancellationToken ct){var q=await Owned(id,true,ct);if(q is null)return false;Draft(q);var e=await db.QuotationCustomsTaxes.SingleOrDefaultAsync(x=>x.QuotationId==id&&x.QuotationCustomsTaxId==tid,ct);if(e is null)return false;if(e.IsCalculated || new[]{"ADVALOREM","IGV","IPM","PERCEPCION"}.Contains(e.TaxCode,StringComparer.OrdinalIgnoreCase))throw new InvalidOperationException("Los tributos automáticos y Ad Valorem no se eliminan desde esta opción.");db.Remove(e);Touch(q);await db.SaveChangesAsync(ct);await Recalc(id,ct);return true;}

    private async Task Recalc(long id,CancellationToken ct)
    {
        var q=await db.Quotations.SingleAsync(x=>x.QuotationId==id,ct);
        await SyncCalculatedCustomsTaxesAsync(q,ct);
        var taxes=await db.QuotationCustomsTaxes.Where(x=>x.QuotationId==id).SumAsync(x=>(decimal?)x.Amount,ct)??0;
        var items=await db.QuotationItems.AsNoTracking().Where(x=>x.QuotationId==id).ToListAsync(ct);
        q.CustomsDutiesAmount=R(taxes); q.ExpenseAmount=R(items.Sum(x=>x.Quantity*x.UnitPrice-x.DiscountAmount)); q.ExpenseTaxAmount=R(items.Sum(x=>x.TaxAmount));
        q.Subtotal=q.ExpenseAmount.Value; q.DiscountAmount=R(items.Sum(x=>x.DiscountAmount)); q.TaxAmount=q.ExpenseTaxAmount.Value;
        q.TotalAmount=QuotationProformaCalculator.Total(q.CustomsDutiesAmount.Value,q.ExpenseAmount.Value,q.ExpenseTaxAmount.Value); Touch(q); await db.SaveChangesAsync(ct);
    }

    private async Task SyncCalculatedCustomsTaxesAsync(Quotation q,CancellationToken ct)
    {
        var concepts=await db.CustomsTaxConcepts.Where(x=>x.IsActive&&new[]{"ADVALOREM","IGV","IPM","PERCEPCION"}.Contains(x.Code)).ToListAsync(ct);
        var byCode=concepts.ToDictionary(x=>x.Code,StringComparer.OrdinalIgnoreCase);
        if(!byCode.TryGetValue("IGV",out var igvConcept)||!byCode.TryGetValue("IPM",out var ipmConcept)||!byCode.TryGetValue("PERCEPCION",out var perceptionConcept)) return;
        if(igvConcept.DefaultRate is null||ipmConcept.DefaultRate is null||perceptionConcept.DefaultRate is null) throw new InvalidOperationException("Las tasas automáticas de derechos aduaneros no están configuradas.");
        var lines=await db.QuotationCustomsTaxes.Where(x=>x.QuotationId==q.QuotationId).ToListAsync(ct);
        var adValorem=byCode.TryGetValue("ADVALOREM",out var adConcept)?lines.FirstOrDefault(x=>x.CustomsTaxConceptId==adConcept.CustomsTaxConceptId)?.Amount??0m:0m;
        var customsValue=q.CustomsValue??QuotationProformaCalculator.CustomsValue(q.ExwAmount??0,q.FreightAmount??0,q.InsuranceAmount??0);
        var igv=QuotationProformaCalculator.CustomsValueTax(customsValue,igvConcept.DefaultRate.Value);
        var ipm=QuotationProformaCalculator.CustomsValueTax(customsValue,ipmConcept.DefaultRate.Value);
        var perceptionBase=QuotationProformaCalculator.PerceptionBase(customsValue,adValorem,igv,ipm);
        var perception=QuotationProformaCalculator.Perception(customsValue,adValorem,igv,ipm,perceptionConcept.DefaultRate.Value);
        UpsertCalculated(lines,q.QuotationId,igvConcept,customsValue,igv);
        UpsertCalculated(lines,q.QuotationId,ipmConcept,customsValue,ipm);
        UpsertCalculated(lines,q.QuotationId,perceptionConcept,perceptionBase,perception);
        await db.SaveChangesAsync(ct);
    }

    private void UpsertCalculated(List<QuotationCustomsTax> lines,long quotationId,CustomsTaxConcept concept,decimal calculationBase,decimal amount)
    {
        var line=lines.FirstOrDefault(x=>x.CustomsTaxConceptId==concept.CustomsTaxConceptId);
        if(line is null){line=new QuotationCustomsTax{QuotationId=quotationId,CustomsTaxConceptId=concept.CustomsTaxConceptId,TaxCode=concept.Code,Description=concept.Name,CurrencyCode="USD",DisplayOrder=concept.DisplayOrder};db.Add(line);lines.Add(line);}
        line.Amount=R(amount); line.RateApplied=concept.DefaultRate; line.CalculationBase=R(calculationBase); line.IsCalculated=true; line.TaxCode=concept.Code; line.Description=concept.Name; line.DisplayOrder=concept.DisplayOrder;
    }
    private async Task<Quotation?> Owned(long id,bool tracked,CancellationToken ct){IQueryable<Quotation> x=db.Quotations.Include(q=>q.CustomsRegimeCatalog).Include(q=>q.CustomsOfficeCatalog);if(!tracked)x=x.AsNoTracking();return await x.SingleOrDefaultAsync(q=>q.QuotationId==id&&q.CompanyId==company.CompanyId&&(!user.IsClientUser||(q.ClientId==user.ClientId&&q.SentAt!=null)),ct);}
    private static void Draft(Quotation q){if(q.Status!=QuotationStatuses.Draft)throw new InvalidOperationException("Solo una cotización DRAFT puede modificarse.");}
    private void Touch(Quotation q){q.ModifiedAt=DateTime.UtcNow;q.ModifiedBy=user.UserId;}
    private static decimal R(decimal x)=>decimal.Round(x,4,MidpointRounding.AwayFromZero); private static string? Clean(string? x)=>string.IsNullOrWhiteSpace(x)?null:x.Trim();
}
