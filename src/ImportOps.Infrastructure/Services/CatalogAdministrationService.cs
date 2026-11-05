using System.Globalization;
using System.Text;
using System.Security.Cryptography;
using ImportOps.Application.Catalogs;
using ImportOps.Application.Common;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Infrastructure.Services;

public sealed class CatalogAdministrationService(ImportOpsDbContext db, ICurrentCompany currentCompany) : ICatalogAdministrationService
{
    private static readonly HashSet<string> IsoCountryCodes = new(StringComparer.OrdinalIgnoreCase) { "AW", "AF", "AO", "AI", "AX", "AL", "AD", "AE", "AR", "AM", "AS", "AQ", "TF", "AG", "AU", "AT", "AZ", "BI", "BE", "BJ", "BQ", "BF", "BD", "BG", "BH", "BS", "BA", "BL", "BY", "BZ", "BM", "BO", "BR", "BB", "BN", "BT", "BV", "BW", "CF", "CA", "CC", "CH", "CL", "CN", "CI", "CM", "CD", "CG", "CK", "CO", "KM", "CV", "CR", "CU", "CW", "CX", "KY", "CY", "CZ", "DE", "DJ", "DM", "DK", "DO", "DZ", "EC", "EG", "ER", "EH", "ES", "EE", "ET", "FI", "FJ", "FK", "FR", "FO", "FM", "GA", "GB", "GE", "GG", "GH", "GI", "GN", "GP", "GM", "GW", "GQ", "GR", "GD", "GL", "GT", "GF", "GU", "GY", "HK", "HM", "HN", "HR", "HT", "HU", "ID", "IM", "IN", "IO", "IE", "IR", "IQ", "IS", "IL", "IT", "JM", "JE", "JO", "JP", "KZ", "KE", "KG", "KH", "KI", "KN", "KR", "KW", "LA", "LB", "LR", "LY", "LC", "LI", "LK", "LS", "LT", "LU", "LV", "MO", "MF", "MA", "MC", "MD", "MG", "MV", "MX", "MH", "MK", "ML", "MT", "MM", "ME", "MN", "MP", "MZ", "MR", "MS", "MQ", "MU", "MW", "MY", "YT", "NA", "NC", "NE", "NF", "NG", "NI", "NU", "NL", "NO", "NP", "NR", "NZ", "OM", "PK", "PA", "PN", "PE", "PH", "PW", "PG", "PL", "PR", "KP", "PT", "PY", "PS", "PF", "QA", "RE", "RO", "RU", "RW", "SA", "SD", "SN", "SG", "GS", "SH", "SJ", "SB", "SL", "SV", "SM", "SO", "PM", "RS", "SS", "ST", "SR", "SK", "SI", "SE", "SZ", "SX", "SC", "SY", "TC", "TD", "TG", "TH", "TJ", "TK", "TM", "TL", "TO", "TT", "TN", "TR", "TV", "TW", "TZ", "UG", "UA", "UM", "UY", "US", "UZ", "VA", "VC", "VE", "VG", "VI", "VN", "VU", "WF", "WS", "YE", "ZA", "ZM", "ZW" };
    private static readonly HashSet<string> IsoCurrencyCodes = new(StringComparer.OrdinalIgnoreCase) { "AED", "AFN", "ALL", "AMD", "ANG", "AOA", "ARS", "AUD", "AWG", "AZN", "BAM", "BBD", "BDT", "BGN", "BHD", "BIF", "BMD", "BND", "BOB", "BOV", "BRL", "BSD", "BTN", "BWP", "BYN", "BZD", "CAD", "CDF", "CHE", "CHF", "CHW", "CLF", "CLP", "CNY", "COP", "COU", "CRC", "CUC", "CUP", "CVE", "CZK", "DJF", "DKK", "DOP", "DZD", "EGP", "ERN", "ETB", "EUR", "FJD", "FKP", "GBP", "GEL", "GHS", "GIP", "GMD", "GNF", "GTQ", "GYD", "HKD", "HNL", "HRK", "HTG", "HUF", "IDR", "ILS", "INR", "IQD", "IRR", "ISK", "JMD", "JOD", "JPY", "KES", "KGS", "KHR", "KMF", "KPW", "KRW", "KWD", "KYD", "KZT", "LAK", "LBP", "LKR", "LRD", "LSL", "LYD", "MAD", "MDL", "MGA", "MKD", "MMK", "MNT", "MOP", "MRU", "MUR", "MVR", "MWK", "MXN", "MXV", "MYR", "MZN", "NAD", "NGN", "NIO", "NOK", "NPR", "NZD", "OMR", "PAB", "PEN", "PGK", "PHP", "PKR", "PLN", "PYG", "QAR", "RON", "RSD", "RUB", "RWF", "SAR", "SBD", "SCR", "SDG", "SEK", "SGD", "SHP", "SLE", "SLL", "SOS", "SRD", "SSP", "STN", "SVC", "SYP", "SZL", "THB", "TJS", "TMT", "TND", "TOP", "TRY", "TTD", "TWD", "TZS", "UAH", "UGX", "USD", "USN", "UYI", "UYU", "UYW", "UZS", "VED", "VES", "VND", "VUV", "WST", "XAF", "XAG", "XAU", "XBA", "XBB", "XBC", "XBD", "XCD", "XDR", "XOF", "XPD", "XPF", "XPT", "XSU", "XTS", "XUA", "XXX", "YER", "ZAR", "ZMW", "ZWL" };
    private static readonly HashSet<string> IncotermCodes = new(StringComparer.OrdinalIgnoreCase) { "EXW", "FCA", "CPT", "CIP", "DAP", "DPU", "DDP", "FAS", "FOB", "CFR", "CIF" };
    private static readonly HashSet<string> DocumentCategoryCodes = new(StringComparer.OrdinalIgnoreCase) { "COMMERCIAL", "LOGISTICS", "CUSTOMS", "FINANCIAL", "OTHER" };

    private static string GenerateInternalCode(string name, int maxLength)
    {
        var normalized = Fold(name);
        var sb = new StringBuilder(normalized.Length);
        var underscore = false;
        foreach (var ch in normalized)
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
                underscore = false;
            }
            else if (!underscore && sb.Length > 0)
            {
                sb.Append('_');
                underscore = true;
            }
        }
        var code = sb.ToString().Trim('_');
        if (code.Length <= maxLength) return code;
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)))[..8];
        return $"{code[..(maxLength - 9)].TrimEnd('_')}_{hash}";
    }

    private static void EnsureImmutableCode(string submitted, string current)
    {
        if (!string.IsNullOrWhiteSpace(submitted) && Key(submitted) != Key(current))
            throw V("El código no puede modificarse después de crear el registro.");
    }

    private static string Key(string value) => value.Trim().ToUpperInvariant();
    private static string Text(string? value) => (value ?? string.Empty).Trim();
    private static string Fold(string value)
    {
        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
        return new string(chars).Normalize(NormalizationForm.FormC).ToUpperInvariant();
    }
    private static void Required(string value, string field, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new BusinessException(ErrorCodes.Validation, $"{field} es obligatorio.", 400);
        if (value.Trim().Length > max) throw new BusinessException(ErrorCodes.Validation, $"{field} no puede exceder {max} caracteres.", 400);
    }
    private static string DocumentCategory(string? value)
    {
        var category = Key(value ?? string.Empty);
        if (!DocumentCategoryCodes.Contains(category))
            throw V("Categoría de documento inválida. Selecciona Comercial, Logística, Aduanas, Financiera u Otros.");
        return category;
    }
    private static (int page, int size) Page(CatalogAdminQuery q)
    {
        if (q.Page < 1) throw new BusinessException(ErrorCodes.Validation, "La página debe ser mayor o igual a 1.", 400);
        if (q.PageSize is < 1 or > 100) throw new BusinessException(ErrorCodes.Validation, "El tamaño de página debe estar entre 1 y 100.", 400);
        return (q.Page, q.PageSize);
    }
    private static bool Status(bool active, string? status) => Key(status ?? "ALL") switch { "ALL" => true, "ACTIVE" => active, "INACTIVE" => !active, _ => throw new BusinessException(ErrorCodes.Validation, "Estado inválido. Use ALL, ACTIVE o INACTIVE.", 400) };
    private static bool Search(string? search, params string?[] values) => string.IsNullOrWhiteSpace(search) || values.Any(v => v != null && Fold(v).Contains(Fold(search)));

    public async Task<PagedResult<CatalogAdminItemDto>> SearchAsync(string catalog, CatalogAdminQuery q, CancellationToken ct)
    {
        var (page, size) = Page(q); var kind = Key(catalog);
        if (kind == "PORTS") return await SearchPortsAsync(q, page, size, ct);
        if (kind == "QUOTATION-CONCEPTS") return await SearchQuotationConceptsAsync(q, page, size, ct);
        IEnumerable<CatalogAdminItemDto> rows = kind switch
        {
            "COUNTRIES" => await db.Countries.AsNoTracking().OrderBy(x=>x.CountryName).Select(x=>new CatalogAdminItemDto(x.CountryCode,x.CountryCode,x.CountryName,x.IsActive,x.Iso3Code,null,null,null,null,null,null,null,null)).ToListAsync(ct),
            "CURRENCIES" => await db.Currencies.AsNoTracking().OrderBy(x=>x.CurrencyCode).Select(x=>new CatalogAdminItemDto(x.CurrencyCode,x.CurrencyCode,x.CurrencyName,x.IsActive,null,x.Symbol,x.DecimalPlaces,null,null,null,null,null,null)).ToListAsync(ct),
            "INCOTERMS" => await db.Incoterms.AsNoTracking().OrderBy(x=>x.IncotermCode).Select(x=>new CatalogAdminItemDto(x.IncotermCode,x.IncotermCode,x.Description,x.IsActive,null,null,null,null,null,null,null,null,null)).ToListAsync(ct),
            "TRANSPORT-MODES" => await db.TransportModes.AsNoTracking().OrderBy(x=>x.Name).Select(x=>new CatalogAdminItemDto(x.TransportModeId.ToString(),x.Code,x.Name,x.IsActive,null,null,null,null,null,null,null,null,null)).ToListAsync(ct),
            "PAYMENT-METHODS" => await db.PaymentMethods.AsNoTracking().OrderBy(x=>x.Name).Select(x=>new CatalogAdminItemDto(x.PaymentMethodId.ToString(),x.Code,x.Name,x.IsActive,null,null,null,null,null,null,null,null,null)).ToListAsync(ct),
            "DOCUMENT-TYPES" => await db.DocumentTypes.AsNoTracking().Where(x=>x.CompanyId==null || x.CompanyId==currentCompany.CompanyId).OrderBy(x=>x.Name).Select(x=>new CatalogAdminItemDto(x.DocumentTypeId.ToString(),x.Code,x.Name,x.IsActive,null,null,null,null,null,null,null,x.Category,x.IsRequiredDefault)).ToListAsync(ct),
            "EXPENSE-CATEGORIES" => await db.ExpenseCategories.AsNoTracking().Where(x=>x.CompanyId==currentCompany.CompanyId).OrderBy(x=>x.Name).Select(x=>new CatalogAdminItemDto(x.ExpenseCategoryId.ToString(),x.Code,x.Name,x.IsActive,null,null,null,null,null,null,null,null,x.IsRebillableDefault)).ToListAsync(ct),
            "SERVICE-CATEGORIES" => await db.ServiceCategories.AsNoTracking().Where(x=>x.CompanyId==currentCompany.CompanyId).OrderBy(x=>x.Name).Select(x=>new CatalogAdminItemDto(x.ServiceCategoryId.ToString(),x.Code,x.Name,x.IsActive,null,null,null,null,null,null,null,null,null)).ToListAsync(ct),
            "CUSTOMS-REGIMES" => await db.CustomsRegimes.AsNoTracking().OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Name).Select(x=>new CatalogAdminItemDto(x.CustomsRegimeId.ToString(),x.Code,x.Name,x.IsActive,null,null,null,null,null,null,null,null,null)).ToListAsync(ct),
            "CUSTOMS-OFFICES" => await db.CustomsOfficeCatalogs.AsNoTracking().OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Name).Select(x=>new CatalogAdminItemDto(x.CustomsOfficeId.ToString(),x.Code,x.Name,x.IsActive,null,null,null,null,null,null,null,null,null)).ToListAsync(ct),
            "CUSTOMS-TAX-CONCEPTS" => await db.CustomsTaxConcepts.AsNoTracking().OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Name).Select(x=>new CatalogAdminItemDto(x.CustomsTaxConceptId.ToString(),x.Code,x.Name,x.IsActive,null,null,null,null,null,null,null,null,null)).ToListAsync(ct),
            _ => throw new BusinessException(ErrorCodes.Validation, "Catálogo no soportado.", 400)
        };
        rows = rows.Where(x => Status(x.IsActive,q.Status) && Search(q.Search,x.Code,x.Name,x.Description,x.CityName));
        var total=rows.Count(); var items=rows.Skip((page-1)*size).Take(size).ToList();
        return new PagedResult<CatalogAdminItemDto>(items,page,size,total);
    }

    private async Task<PagedResult<CatalogAdminItemDto>> SearchQuotationConceptsAsync(CatalogAdminQuery q,int page,int size,CancellationToken ct)
    {
        var query=db.QuotationConcepts.AsNoTracking().Where(x=>x.CompanyId==currentCompany.CompanyId);
        var status=Key(q.Status??"ALL");
        if(status=="ACTIVE")query=query.Where(x=>x.IsActive);else if(status=="INACTIVE")query=query.Where(x=>!x.IsActive);else if(status!="ALL")throw V("Estado inválido. Use ALL, ACTIVE o INACTIVE.");
        if(!string.IsNullOrWhiteSpace(q.Search)){var term=q.Search.Trim();query=query.Where(x=>x.Code.Contains(term)||x.Name.Contains(term)||x.GroupName.Contains(term));}
        var total=await query.CountAsync(ct);
        var rows=await query.OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.Name).Skip((page-1)*size).Take(size).ToListAsync(ct);
        var items=rows.Select(x=>new CatalogAdminItemDto(x.QuotationConceptId.ToString(),x.Code,x.Name,x.IsActive)
        {ServiceCategoryId=x.ServiceCategoryId,ExpenseCategoryId=x.ExpenseCategoryId,DefaultTaxRate=x.DefaultTaxRate,IsTaxable=x.IsTaxable,IsRebillableDefault=x.IsRebillableDefault,DisplayOrder=x.DisplayOrder,GroupCode=x.GroupCode,GroupName=x.GroupName}).ToList();
        return new PagedResult<CatalogAdminItemDto>(items,page,size,total);
    }

    private async Task<PagedResult<CatalogAdminItemDto>> SearchPortsAsync(CatalogAdminQuery q, int page, int size, CancellationToken ct)
    {
        var query = from p in db.Ports.AsNoTracking() join c in db.Countries.AsNoTracking() on p.CountryCode equals c.CountryCode select new { p, c.CountryName };
        var status = Key(q.Status ?? "ALL");
        if (status == "ACTIVE") query = query.Where(x => x.p.IsActive);
        else if (status == "INACTIVE") query = query.Where(x => !x.p.IsActive);
        else if (status != "ALL") throw V("Estado inválido. Use ALL, ACTIVE o INACTIVE.");
        if (!string.IsNullOrWhiteSpace(q.CountryCode)) { var country = Key(q.CountryCode); query = query.Where(x => x.p.CountryCode == country); }
        if (!string.IsNullOrWhiteSpace(q.PortType)) { var type = Key(q.PortType); if (type is not ("SEA" or "AIR")) throw V("Tipo de puerto inválido."); query = query.Where(x => x.p.PortType == type); }
        if (!string.IsNullOrWhiteSpace(q.Search)) { var term = q.Search.Trim(); query = query.Where(x => x.p.PortName.Contains(term) || (x.p.PortCode != null && x.p.PortCode.Contains(term)) || (x.p.CityName != null && x.p.CityName.Contains(term))); }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.p.PortName).ThenBy(x => x.p.PortId).Skip((page - 1) * size).Take(size)
            .Select(x => new CatalogAdminItemDto(x.p.PortId.ToString(), x.p.PortCode ?? x.p.PortId.ToString(), x.p.PortName, x.p.IsActive, null, null, null, x.p.CountryCode, x.CountryName, x.p.CityName, x.p.PortType, null, null)).ToListAsync(ct);
        return new PagedResult<CatalogAdminItemDto>(items, page, size, total);
    }

    public async Task<CatalogAdminItemDto> CreateAsync(string catalog, CatalogAdminSaveRequest r, CancellationToken ct)
    {
        var kind=Key(catalog); var code=Key(r.Code ?? ""); var name=Text(r.Name);
        switch(kind)
        {
            case "COUNTRIES":
                Required(code,"Código ISO",2); if(code.Length!=2 || !IsoCountryCodes.Contains(code)) throw V("Seleccione un código ISO 3166-1 válido para el país."); Required(name,"Nombre",120); await UniqueCountry(code,name,null,ct); db.Countries.Add(new Country{CountryCode=code,CountryName=name,Iso3Code=string.IsNullOrWhiteSpace(r.Description)?null:Key(r.Description),IsActive=true}); break;
            case "CURRENCIES":
                Required(code,"Código ISO",3); if(code.Length!=3 || !IsoCurrencyCodes.Contains(code)) throw V("Seleccione un código ISO 4217 válido para la moneda."); Required(name,"Nombre",100); if(r.DecimalPlaces is <0 or >6) throw V("Los decimales deben estar entre 0 y 6."); await UniqueCurrency(code,name,null,ct); db.Currencies.Add(new Currency{CurrencyCode=code,CurrencyName=name,Symbol=Text(r.Symbol) is {Length:>0} s?s:null,DecimalPlaces=(byte)(r.DecimalPlaces??2),IsActive=true}); break;
            case "INCOTERMS": Required(code,"Código Incoterm",3); if(!IncotermCodes.Contains(code)) throw V("Seleccione un Incoterm 2020 válido."); Required(name,"Descripción",200); if(await db.Incoterms.AnyAsync(x=>x.IncotermCode==code,ct)) throw D("incoterm",code); db.Incoterms.Add(new Incoterm{IncotermCode=code,Description=name,IsActive=true}); break;
            case "TRANSPORT-MODES": Required(name,"Nombre",100); code=GenerateInternalCode(name,30); Required(code,"Código interno",30); await UniqueSimple(db.TransportModes.Select(x=>new {x.TransportModeId,x.Code,x.Name}),code,name,null,"modo de transporte",ct); db.TransportModes.Add(new TransportMode{Code=code,Name=name,IsActive=true}); break;
            case "PAYMENT-METHODS": Required(name,"Nombre",100); code=GenerateInternalCode(name,30); Required(code,"Código interno",30); await UniqueSimple(db.PaymentMethods.Select(x=>new {Id=x.PaymentMethodId,x.Code,x.Name}),code,name,null,"método de pago",ct); db.PaymentMethods.Add(new PaymentMethod{Code=code,Name=name,IsActive=true}); break;
            case "DOCUMENT-TYPES": Required(name,"Nombre",120); code=GenerateInternalCode(name,40); Required(code,"Código interno",40); var documentCategory=DocumentCategory(r.Category); await UniqueCompany(db.DocumentTypes.Where(x=>x.CompanyId==currentCompany.CompanyId),code,name,null,"tipo de documento",ct); db.DocumentTypes.Add(new DocumentType{CompanyId=currentCompany.CompanyId,Code=code,Name=name,Category=documentCategory,IsRequiredDefault=r.Flag??false,IsActive=true}); break;
            case "EXPENSE-CATEGORIES": Required(name,"Nombre",120); code=GenerateInternalCode(name,40); Required(code,"Código interno",40); await UniqueCompany(db.ExpenseCategories.Where(x=>x.CompanyId==currentCompany.CompanyId),code,name,null,"categoría de gasto",ct); db.ExpenseCategories.Add(new ExpenseCategory{CompanyId=currentCompany.CompanyId,Code=code,Name=name,IsRebillableDefault=r.Flag??false,IsActive=true}); break;
            case "SERVICE-CATEGORIES": Required(name,"Nombre",120); code=GenerateInternalCode(name,40); Required(code,"Código interno",40); await UniqueCompany(db.ServiceCategories.Where(x=>x.CompanyId==currentCompany.CompanyId),code,name,null,"categoría de servicio",ct); db.ServiceCategories.Add(new ServiceCategory{CompanyId=currentCompany.CompanyId,Code=code,Name=name,IsActive=true}); break;
            case "CUSTOMS-REGIMES": Required(name,"Nombre",120); code=GenerateInternalCode(name,30); await UniqueSelectable(db.CustomsRegimes.Select(x=>new {Id=x.CustomsRegimeId,x.Code,x.Name}),code,name,null,"régimen aduanero",ct); db.CustomsRegimes.Add(new CustomsRegime{Code=code,Name=name,DisplayOrder=10,IsActive=true,CreatedAt=DateTime.UtcNow}); break;
            case "CUSTOMS-OFFICES": Required(name,"Nombre",160); code=GenerateInternalCode(name,30); await UniqueSelectable(db.CustomsOfficeCatalogs.Select(x=>new {Id=x.CustomsOfficeId,x.Code,x.Name}),code,name,null,"aduana",ct); db.CustomsOfficeCatalogs.Add(new CustomsOfficeCatalog{Code=code,Name=name,DisplayOrder=10,IsActive=true,CreatedAt=DateTime.UtcNow}); break;
            case "CUSTOMS-TAX-CONCEPTS": Required(name,"Nombre",120); code=GenerateInternalCode(name,40); await UniqueSelectable(db.CustomsTaxConcepts.Select(x=>new {Id=x.CustomsTaxConceptId,x.Code,x.Name}),code,name,null,"concepto aduanero",ct); db.CustomsTaxConcepts.Add(new CustomsTaxConcept{Code=code,Name=name,DisplayOrder=10,IsActive=true,CreatedAt=DateTime.UtcNow}); break;
            case "QUOTATION-CONCEPTS": { Required(name,"Nombre",160); code=GenerateInternalCode(name,50); await ValidateQuotationConceptAsync(r,null,ct); if(await db.QuotationConcepts.AnyAsync(x=>x.CompanyId==currentCompany.CompanyId && (x.Code==code || x.Name==name),ct)) throw V("Ya existe un concepto de proforma con el mismo código o nombre."); db.QuotationConcepts.Add(new QuotationConcept{CompanyId=currentCompany.CompanyId,Code=code,Name=name,GroupCode=Key(r.GroupCode!),GroupName=Text(r.GroupName),ServiceCategoryId=r.ServiceCategoryId,ExpenseCategoryId=r.ExpenseCategoryId,DefaultTaxRate=r.IsTaxable==true?r.DefaultTaxRate??0:0,IsTaxable=r.IsTaxable??false,IsRebillableDefault=r.IsRebillableDefault??false,DisplayOrder=r.DisplayOrder??10,IsActive=true,CreatedAt=DateTime.UtcNow}); break; }
            case "PORTS": await ValidatePort(r,null,ct); db.Ports.Add(new Port{CountryCode=Key(r.CountryCode!),PortCode=string.IsNullOrWhiteSpace(r.Code)?null:Key(r.Code),PortName=name,CityName=Text(r.CityName) is {Length:>0} city?city:null,PortType=Key(r.PortType!),IsActive=true}); break;
            default: throw V("Catálogo no soportado.");
        }
        await db.SaveChangesAsync(ct);
        var created = await SearchAsync(catalog,new CatalogAdminQuery(1,100,r.Code ?? r.Name,"ALL",r.CountryCode,r.PortType),ct);
        return created.Items.FirstOrDefault() ?? throw new InvalidOperationException("No se pudo recuperar el catálogo creado.");
    }

    public async Task<CatalogAdminItemDto> UpdateAsync(string catalog,string id,CatalogAdminSaveRequest r,CancellationToken ct)
    {
        var kind=Key(catalog); var code=Key(r.Code ?? ""); var name=Text(r.Name);
        switch(kind)
        {
            case "COUNTRIES": { var e=await db.Countries.FindAsync([Key(id)],ct)??throw N(); EnsureImmutableCode(r.Code ?? e.CountryCode, e.CountryCode); Required(name,"Nombre",120); await UniqueCountry(e.CountryCode,name,e.CountryCode,ct); e.CountryName=name; e.Iso3Code=string.IsNullOrWhiteSpace(r.Description)?null:Key(r.Description); break; }
            case "CURRENCIES": { var e=await db.Currencies.FindAsync([Key(id)],ct)??throw N(); EnsureImmutableCode(r.Code ?? e.CurrencyCode, e.CurrencyCode); Required(name,"Nombre",100); await UniqueCurrency(e.CurrencyCode,name,e.CurrencyCode,ct); e.CurrencyName=name;e.Symbol=Text(r.Symbol) is {Length:>0}s?s:null;e.DecimalPlaces=(byte)(r.DecimalPlaces??e.DecimalPlaces);break; }
            case "INCOTERMS": { var e=await db.Incoterms.FindAsync([Key(id)],ct)??throw N(); EnsureImmutableCode(r.Code ?? e.IncotermCode, e.IncotermCode); Required(name,"Descripción",200); e.Description=name;break; }
            case "TRANSPORT-MODES": { var e=await FindInt(db.TransportModes,id,ct); EnsureImmutableCode(r.Code ?? e.Code,e.Code); code=e.Code; Required(name,"Nombre",100);await UniqueSimple(db.TransportModes.Select(x=>new {x.TransportModeId,x.Code,x.Name}),code,name,e.TransportModeId,"modo de transporte",ct);e.Name=name;break; }
            case "PAYMENT-METHODS": { var e=await FindInt(db.PaymentMethods,id,ct); EnsureImmutableCode(r.Code ?? e.Code,e.Code); code=e.Code;Required(name,"Nombre",100);await UniqueSimple(db.PaymentMethods.Select(x=>new {Id=x.PaymentMethodId,x.Code,x.Name}),code,name,e.PaymentMethodId,"método de pago",ct);e.Name=name;break; }
            case "DOCUMENT-TYPES": { var e=await FindInt(db.DocumentTypes,id,ct); EnsureCompany(e.CompanyId);EnsureImmutableCode(r.Code ?? e.Code,e.Code); code=e.Code;Required(name,"Nombre",120);var documentCategory=DocumentCategory(r.Category);await UniqueCompany(db.DocumentTypes.Where(x=>x.CompanyId==currentCompany.CompanyId),code,name,e.DocumentTypeId,"tipo de documento",ct);e.Name=name;e.Category=documentCategory;e.IsRequiredDefault=r.Flag??e.IsRequiredDefault;break; }
            case "EXPENSE-CATEGORIES": { var e=await FindInt(db.ExpenseCategories,id,ct);EnsureCompany(e.CompanyId);EnsureImmutableCode(r.Code ?? e.Code,e.Code); code=e.Code;Required(name,"Nombre",120);await UniqueCompany(db.ExpenseCategories.Where(x=>x.CompanyId==currentCompany.CompanyId),code,name,e.ExpenseCategoryId,"categoría de gasto",ct);e.Name=name;e.IsRebillableDefault=r.Flag??e.IsRebillableDefault;break; }
            case "SERVICE-CATEGORIES": { var e=await FindInt(db.ServiceCategories,id,ct);EnsureCompany(e.CompanyId);EnsureImmutableCode(r.Code ?? e.Code,e.Code); code=e.Code;Required(name,"Nombre",120);await UniqueCompany(db.ServiceCategories.Where(x=>x.CompanyId==currentCompany.CompanyId),code,name,e.ServiceCategoryId,"categoría de servicio",ct);e.Name=name;break; }
            case "CUSTOMS-REGIMES": {var e=await FindInt(db.CustomsRegimes,id,ct);EnsureImmutableCode(r.Code??e.Code,e.Code);Required(name,"Nombre",120);await UniqueSelectable(db.CustomsRegimes.Select(x=>new {Id=x.CustomsRegimeId,x.Code,x.Name}),e.Code,name,e.CustomsRegimeId,"régimen aduanero",ct);e.Name=name;e.ModifiedAt=DateTime.UtcNow;break;}
            case "CUSTOMS-OFFICES": {var e=await FindInt(db.CustomsOfficeCatalogs,id,ct);EnsureImmutableCode(r.Code??e.Code,e.Code);Required(name,"Nombre",160);await UniqueSelectable(db.CustomsOfficeCatalogs.Select(x=>new {Id=x.CustomsOfficeId,x.Code,x.Name}),e.Code,name,e.CustomsOfficeId,"aduana",ct);e.Name=name;e.ModifiedAt=DateTime.UtcNow;break;}
            case "CUSTOMS-TAX-CONCEPTS": {var e=await FindInt(db.CustomsTaxConcepts,id,ct);EnsureImmutableCode(r.Code??e.Code,e.Code);Required(name,"Nombre",120);await UniqueSelectable(db.CustomsTaxConcepts.Select(x=>new {Id=x.CustomsTaxConceptId,x.Code,x.Name}),e.Code,name,e.CustomsTaxConceptId,"concepto aduanero",ct);e.Name=name;e.ModifiedAt=DateTime.UtcNow;break;}
            case "QUOTATION-CONCEPTS": {var k=Long(id);var e=await db.QuotationConcepts.SingleOrDefaultAsync(x=>x.QuotationConceptId==k&&x.CompanyId==currentCompany.CompanyId,ct)??throw N();EnsureImmutableCode(r.Code??e.Code,e.Code);Required(name,"Nombre",160);await ValidateQuotationConceptAsync(r,e.QuotationConceptId,ct);if(await db.QuotationConcepts.AnyAsync(x=>x.CompanyId==currentCompany.CompanyId&&x.QuotationConceptId!=e.QuotationConceptId&&x.Name==name,ct))throw V("Ya existe un concepto de proforma con el mismo nombre.");e.Name=name;e.GroupCode=Key(r.GroupCode??e.GroupCode);e.GroupName=Text(r.GroupName) is {Length:>0} gn?gn:e.GroupName;e.ServiceCategoryId=r.ServiceCategoryId;e.ExpenseCategoryId=r.ExpenseCategoryId;e.IsTaxable=r.IsTaxable??e.IsTaxable;e.DefaultTaxRate=e.IsTaxable?r.DefaultTaxRate??e.DefaultTaxRate:0;e.IsRebillableDefault=r.IsRebillableDefault??e.IsRebillableDefault;e.DisplayOrder=r.DisplayOrder??e.DisplayOrder;e.ModifiedAt=DateTime.UtcNow;break;}
            case "PORTS": { var e=await FindLong(db.Ports,id,ct);await ValidatePort(r,e.PortId,ct);EnsureImmutableCode(r.Code ?? e.PortCode ?? "",e.PortCode??"");e.CountryCode=Key(r.CountryCode!);e.PortCode=string.IsNullOrWhiteSpace(r.Code)?null:Key(r.Code);e.PortName=name;e.CityName=Text(r.CityName) is {Length:>0}city?city:null;e.PortType=Key(r.PortType!);break; }
            default: throw V("Catálogo no soportado.");
        }
        await db.SaveChangesAsync(ct); var rows=await SearchAsync(catalog,new CatalogAdminQuery(1,100,r.Code??r.Name,"ALL",r.CountryCode,r.PortType),ct);return rows.Items.FirstOrDefault()??throw N();
    }

    public async Task SetActiveAsync(string catalog,string id,bool active,CancellationToken ct)
    {
        switch(Key(catalog))
        {
            case "COUNTRIES": (await db.Countries.FindAsync([Key(id)],ct)??throw N()).IsActive=active;break;
            case "CURRENCIES": (await db.Currencies.FindAsync([Key(id)],ct)??throw N()).IsActive=active;break;
            case "INCOTERMS": (await db.Incoterms.FindAsync([Key(id)],ct)??throw N()).IsActive=active;break;
            case "PORTS": (await FindLong(db.Ports,id,ct)).IsActive=active;break;
            case "TRANSPORT-MODES": (await FindInt(db.TransportModes,id,ct)).IsActive=active;break;
            case "PAYMENT-METHODS": (await FindInt(db.PaymentMethods,id,ct)).IsActive=active;break;
            case "DOCUMENT-TYPES": {var e=await FindInt(db.DocumentTypes,id,ct);EnsureCompany(e.CompanyId);e.IsActive=active;break;}
            case "EXPENSE-CATEGORIES": {var e=await FindInt(db.ExpenseCategories,id,ct);EnsureCompany(e.CompanyId);e.IsActive=active;break;}
            case "SERVICE-CATEGORIES": {var e=await FindInt(db.ServiceCategories,id,ct);EnsureCompany(e.CompanyId);e.IsActive=active;break;}
            case "CUSTOMS-REGIMES": {var e=await FindInt(db.CustomsRegimes,id,ct);e.IsActive=active;e.ModifiedAt=DateTime.UtcNow;break;}
            case "CUSTOMS-OFFICES": {var e=await FindInt(db.CustomsOfficeCatalogs,id,ct);e.IsActive=active;e.ModifiedAt=DateTime.UtcNow;break;}
            case "CUSTOMS-TAX-CONCEPTS": {var e=await FindInt(db.CustomsTaxConcepts,id,ct);e.IsActive=active;e.ModifiedAt=DateTime.UtcNow;break;}
            case "QUOTATION-CONCEPTS": {var k=Long(id);var e=await db.QuotationConcepts.SingleOrDefaultAsync(x=>x.QuotationConceptId==k&&x.CompanyId==currentCompany.CompanyId,ct)??throw N();e.IsActive=active;e.ModifiedAt=DateTime.UtcNow;break;}
            default: throw V("Catálogo no soportado.");
        } await db.SaveChangesAsync(ct);
    }

    public async Task<CatalogDeleteCheckDto> CheckDeleteAsync(string catalog,string id,CancellationToken ct)
    {
        var u=new List<CatalogUsageDto>(); var kind=Key(catalog);
        void Add(string area,int count){if(count>0)u.Add(new(area,count));}
        switch(kind)
        {
            case "COUNTRIES": {var k=Key(id);if(!await db.Countries.AnyAsync(x=>x.CountryCode==k,ct))throw N();Add("Puertos",await db.Ports.CountAsync(x=>x.CountryCode==k,ct));Add("Solicitudes",await db.ImportRequests.CountAsync(x=>x.OriginCountryCode==k||x.DestinationCountryCode==k,ct));Add("Operaciones",await db.ImportOperations.CountAsync(x=>x.OriginCountryCode==k||x.DestinationCountryCode==k,ct));Add("Clientes",await db.Clients.CountAsync(x=>x.CountryCode==k,ct));Add("Proveedores",await db.Suppliers.CountAsync(x=>x.CountryCode==k,ct));break;}
            case "CURRENCIES": {var k=Key(id);if(!await db.Currencies.AnyAsync(x=>x.CurrencyCode==k,ct))throw N();Add("Cotizaciones",await db.Quotations.CountAsync(x=>x.CurrencyCode==k,ct));Add("Solicitudes",await db.ImportRequests.CountAsync(x=>x.DeclaredCurrencyCode==k,ct));Add("Operaciones",await db.ImportOperations.CountAsync(x=>x.DeclaredCurrencyCode==k,ct));Add("Gastos",await db.OperationExpenses.CountAsync(x=>x.CurrencyCode==k,ct));Add("Servicios",await db.OperationServices.CountAsync(x=>x.CurrencyCode==k,ct));break;}
            case "INCOTERMS": {var k=Key(id);if(!await db.Incoterms.AnyAsync(x=>x.IncotermCode==k,ct))throw N();Add("Solicitudes",await db.ImportRequests.CountAsync(x=>x.IncotermCode==k,ct));Add("Operaciones",await db.ImportOperations.CountAsync(x=>x.IncotermCode==k,ct));break;}
            case "PORTS": {var k=Long(id);if(!await db.Ports.AnyAsync(x=>x.PortId==k,ct))throw N();Add("Solicitudes",await db.ImportRequests.CountAsync(x=>x.OriginPortId==k||x.DestinationPortId==k,ct));Add("Operaciones",await db.ImportOperations.CountAsync(x=>x.OriginPortId==k||x.DestinationPortId==k,ct));Add("Embarques",await db.Shipments.CountAsync(x=>x.OriginPortId==k||x.DestinationPortId==k,ct));break;}
            case "TRANSPORT-MODES": {var k=Int(id);if(!await db.TransportModes.AnyAsync(x=>x.TransportModeId==k,ct))throw N();Add("Solicitudes",await db.ImportRequests.CountAsync(x=>x.TransportModeId==k,ct));Add("Operaciones",await db.ImportOperations.CountAsync(x=>x.TransportModeId==k,ct));Add("Embarques",await db.Shipments.CountAsync(x=>x.TransportModeId==k,ct));break;}
            case "PAYMENT-METHODS": {var k=Int(id);if(!await db.PaymentMethods.AnyAsync(x=>x.PaymentMethodId==k,ct))throw N();Add("Fondos",await db.ClientFunds.CountAsync(x=>x.PaymentMethodId==k,ct));Add("Cobranzas",await db.ClientCollections.CountAsync(x=>x.PaymentMethodId==k,ct));Add("Reembolsos",await db.ClientRefunds.CountAsync(x=>x.PaymentMethodId==k,ct));break;}
            case "DOCUMENT-TYPES": {var k=Int(id);var e=await db.DocumentTypes.FindAsync([k],ct)??throw N();EnsureCompany(e.CompanyId);Add("Documentos",await db.Documents.CountAsync(x=>x.DocumentTypeId==k,ct));Add("Requisitos documentales",await db.StageDocumentRequirements.CountAsync(x=>x.DocumentTypeId==k,ct));break;}
            case "EXPENSE-CATEGORIES": {var k=Int(id);var e=await db.ExpenseCategories.FindAsync([k],ct)??throw N();EnsureCompany(e.CompanyId);Add("Gastos",await db.OperationExpenses.CountAsync(x=>x.ExpenseCategoryId==k,ct));Add("Ítems de cotización",await db.QuotationItems.CountAsync(x=>x.ExpenseCategoryId==k,ct));break;}
            case "SERVICE-CATEGORIES": {var k=Int(id);var e=await db.ServiceCategories.FindAsync([k],ct)??throw N();EnsureCompany(e.CompanyId);Add("Servicios",await db.OperationServices.CountAsync(x=>x.ServiceCategoryId==k,ct));Add("Ítems de cotización",await db.QuotationItems.CountAsync(x=>x.ServiceCategoryId==k,ct));break;}
            case "CUSTOMS-REGIMES": {var k=Int(id);if(!await db.CustomsRegimes.AnyAsync(x=>x.CustomsRegimeId==k,ct))throw N();Add("Cotizaciones",await db.Quotations.CountAsync(x=>x.CustomsRegimeId==k,ct));Add("Procesos aduaneros",await db.CustomsProcesses.CountAsync(x=>x.CustomsRegimeId==k,ct));break;}
            case "CUSTOMS-OFFICES": {var k=Int(id);if(!await db.CustomsOfficeCatalogs.AnyAsync(x=>x.CustomsOfficeId==k,ct))throw N();Add("Cotizaciones",await db.Quotations.CountAsync(x=>x.CustomsOfficeId==k,ct));Add("Procesos aduaneros",await db.CustomsProcesses.CountAsync(x=>x.CustomsOfficeId==k,ct));break;}
            case "CUSTOMS-TAX-CONCEPTS": {var k=Int(id);if(!await db.CustomsTaxConcepts.AnyAsync(x=>x.CustomsTaxConceptId==k,ct))throw N();Add("Derechos estimados",await db.QuotationCustomsTaxes.CountAsync(x=>x.CustomsTaxConceptId==k,ct));Add("Tributos reales",await db.CustomsTaxes.CountAsync(x=>x.CustomsTaxConceptId==k,ct));break;}
            case "QUOTATION-CONCEPTS": {var k=Long(id);if(!await db.QuotationConcepts.AnyAsync(x=>x.QuotationConceptId==k&&x.CompanyId==currentCompany.CompanyId,ct))throw N();Add("Ítems de cotización",await db.QuotationItems.CountAsync(x=>x.QuotationConceptId==k,ct));break;}
            default: throw V("Catálogo no soportado.");
        }
        return new(u.Count==0,u,u.Count==0?"El registro nunca ha sido utilizado y puede eliminarse definitivamente.":"Este registro está siendo utilizado por información registrada en ImportOps y debe conservarse. Puedes desactivarlo para impedir su uso en nuevos registros.");
    }

    public async Task DeleteAsync(string catalog,string id,CancellationToken ct)
    {
        var check=await CheckDeleteAsync(catalog,id,ct);if(!check.CanDelete)throw new BusinessException(ErrorCodes.BusinessRule,check.Message,409);
        switch(Key(catalog)){case "COUNTRIES":db.Countries.Remove((await db.Countries.FindAsync([Key(id)],ct))!);break;case "CURRENCIES":db.Currencies.Remove((await db.Currencies.FindAsync([Key(id)],ct))!);break;case "INCOTERMS":db.Incoterms.Remove((await db.Incoterms.FindAsync([Key(id)],ct))!);break;case "PORTS":db.Ports.Remove(await FindLong(db.Ports,id,ct));break;case "TRANSPORT-MODES":db.TransportModes.Remove(await FindInt(db.TransportModes,id,ct));break;case "PAYMENT-METHODS":db.PaymentMethods.Remove(await FindInt(db.PaymentMethods,id,ct));break;case "DOCUMENT-TYPES":db.DocumentTypes.Remove(await FindInt(db.DocumentTypes,id,ct));break;case "EXPENSE-CATEGORIES":db.ExpenseCategories.Remove(await FindInt(db.ExpenseCategories,id,ct));break;case "SERVICE-CATEGORIES":db.ServiceCategories.Remove(await FindInt(db.ServiceCategories,id,ct));break;case "CUSTOMS-REGIMES":db.CustomsRegimes.Remove(await FindInt(db.CustomsRegimes,id,ct));break;case "CUSTOMS-OFFICES":db.CustomsOfficeCatalogs.Remove(await FindInt(db.CustomsOfficeCatalogs,id,ct));break;case "CUSTOMS-TAX-CONCEPTS":db.CustomsTaxConcepts.Remove(await FindInt(db.CustomsTaxConcepts,id,ct));break;case "QUOTATION-CONCEPTS":{var k=Long(id);var e=await db.QuotationConcepts.SingleOrDefaultAsync(x=>x.QuotationConceptId==k&&x.CompanyId==currentCompany.CompanyId,ct)??throw N();db.QuotationConcepts.Remove(e);break;}default:throw V("Catálogo no soportado.");}await db.SaveChangesAsync(ct);
    }

    private async Task ValidateQuotationConceptAsync(CatalogAdminSaveRequest r,long? self,CancellationToken ct)
    {
        if (r.ServiceCategoryId.HasValue == r.ExpenseCategoryId.HasValue) throw V("El concepto debe tener exactamente una categoría: servicio o gasto.");
        var group=Key(r.GroupCode??""); if(group is not ("FREIGHT_INSURANCE" or "LOCAL_EXPENSES" or "CUSTOMS_AGENCY" or "TRANSPORT" or "OTHER")) throw V("Selecciona un grupo válido para el concepto.");
        if(string.IsNullOrWhiteSpace(r.GroupName)) throw V("El nombre del grupo es obligatorio.");
        if (r.DefaultTaxRate is < 0 or > 100) throw V("La tasa de impuesto debe estar entre 0 y 100.");
        if (r.DisplayOrder is < 0) throw V("El orden no puede ser negativo.");
        if (r.ServiceCategoryId.HasValue && !await db.ServiceCategories.AnyAsync(x=>x.ServiceCategoryId==r.ServiceCategoryId.Value&&x.CompanyId==currentCompany.CompanyId&&x.IsActive,ct)) throw V("La categoría de servicio no existe o está inactiva.");
        if (r.ExpenseCategoryId.HasValue && !await db.ExpenseCategories.AnyAsync(x=>x.ExpenseCategoryId==r.ExpenseCategoryId.Value&&x.CompanyId==currentCompany.CompanyId&&x.IsActive,ct)) throw V("La categoría de gasto no existe o está inactiva.");
    }

    private async Task ValidatePort(CatalogAdminSaveRequest r,long? self,CancellationToken ct){var country=Key(r.CountryCode??"");var type=Key(r.PortType??"");var name=Text(r.Name);Required(country,"País",2);Required(name,"Nombre",150);if(type is not("SEA" or "AIR"))throw V("El tipo de puerto debe ser SEA o AIR.");if(!await db.Countries.AnyAsync(x=>x.CountryCode==country&&x.IsActive,ct))throw V("El país seleccionado no existe o está inactivo.");var code=string.IsNullOrWhiteSpace(r.Code)?null:Key(r.Code);var rows=await db.Ports.Where(x=>x.CountryCode==country && (!self.HasValue||x.PortId!=self.Value)).Select(x=>new{x.PortCode,x.PortName,x.CityName,x.PortType}).ToListAsync(ct);if(code!=null&&rows.Any(x=>x.PortCode==code))throw D("puerto",code);if(rows.Any(x=>Fold(x.PortName)==Fold(name)&&Fold(x.CityName??"")==Fold(Text(r.CityName))&&x.PortType==type))throw V("Ya existe un puerto con el mismo país, nombre, ciudad y tipo.");}
    private async Task UniqueCountry(string code,string name,string? self,CancellationToken ct){if(await db.Countries.AnyAsync(x=>x.CountryCode==code&&x.CountryCode!=self,ct))throw D("país",code);var rows=await db.Countries.Where(x=>x.CountryCode!=self).Select(x=>x.CountryName).ToListAsync(ct);if(rows.Any(x=>Fold(x)==Fold(name)))throw V($"Ya existe un país llamado '{name}'.");}
    private async Task UniqueCurrency(string code,string name,string? self,CancellationToken ct){if(await db.Currencies.AnyAsync(x=>x.CurrencyCode==code&&x.CurrencyCode!=self,ct))throw D("moneda",code);var rows=await db.Currencies.Where(x=>x.CurrencyCode!=self).Select(x=>x.CurrencyName).ToListAsync(ct);if(rows.Any(x=>Fold(x)==Fold(name)))throw V($"Ya existe una moneda llamada '{name}'.");}
    private static async Task UniqueSimple<T>(IQueryable<T> q,string code,string name,int? self,string label,CancellationToken ct){var rows=await q.ToListAsync(ct);foreach(var x in rows){dynamic d=x!;int id;try{id=d.TransportModeId;}catch{id=d.Id;}if(self==id)continue;if(Key((string)d.Code)==code)throw D(label,code);if(Fold((string)d.Name)==Fold(name))throw V($"Ya existe un {label} llamado '{name}'.");}}
    private static async Task UniqueCompany<T>(IQueryable<T> q,string code,string name,int? self,string label,CancellationToken ct){var rows=await q.ToListAsync(ct);foreach(var x in rows){dynamic d=x!;int id=0;try{id=d.DocumentTypeId;}catch{try{id=d.ExpenseCategoryId;}catch{id=d.ServiceCategoryId;}}if(self==id)continue;if(Key((string)d.Code)==code)throw D(label,code);if(Fold((string)d.Name)==Fold(name))throw V($"Ya existe un {label} llamado '{name}'.");}}
    private static async Task UniqueSelectable<T>(IQueryable<T> q,string code,string name,int? self,string label,CancellationToken ct){var rows=await q.ToListAsync(ct);foreach(var x in rows){dynamic d=x!;int id=d.Id;if(self==id)continue;if(Key((string)d.Code)==code)throw D(label,code);if(Fold((string)d.Name)==Fold(name))throw V($"Ya existe un {label} llamado '{name}'.");}}
    private void EnsureCompany(long? companyId){if(companyId!=currentCompany.CompanyId)throw new BusinessException(ErrorCodes.NotFound,"Registro no encontrado.",404);}
    private static int Int(string id)=>int.TryParse(id,out var x)?x:throw V("Identificador inválido.");private static long Long(string id)=>long.TryParse(id,out var x)?x:throw V("Identificador inválido.");
    private static async Task<T> FindInt<T>(DbSet<T> set,string id,CancellationToken ct) where T:class => await set.FindAsync([Int(id)],ct)??throw N();private static async Task<T> FindLong<T>(DbSet<T> set,string id,CancellationToken ct) where T:class => await set.FindAsync([Long(id)],ct)??throw N();
    private static BusinessException V(string m)=>new(ErrorCodes.Validation,m,400);private static BusinessException D(string label,string code)=>new(ErrorCodes.BusinessRule,$"Ya existe un {label} con el código '{code}'.",409);private static BusinessException N()=>new(ErrorCodes.NotFound,"Registro no encontrado.",404);
}
