using ImportOps.Api.Security;
using ImportOps.Application.Security;
using ImportOps.Application.Quotations;
using Microsoft.AspNetCore.Mvc;
namespace ImportOps.Api.Controllers;

[ApiController, Route("api/quotations")]
[RequirePermission(Permissions.QuotationsView)]
public sealed class QuotationProformaController(IQuotationProformaService service):ControllerBase
{
 [HttpGet("{id:long}/proforma")] public async Task<IActionResult> Get(long id,CancellationToken ct){var x=await service.GetAsync(id,ct);return x is null?NotFound():Ok(x);}
 [HttpGet("{id:long}/pdf")] public async Task<IActionResult> Pdf(long id,CancellationToken ct){var x=await service.GeneratePdfAsync(id,ct);return x is null?NotFound():File(x.Value.Content,"application/pdf",x.Value.FileName);}
 [HttpPut("{id:long}/proforma"),RequirePermission(Permissions.QuotationsManage)] public async Task<IActionResult> Update(long id,UpdateQuotationProformaRequest r,CancellationToken ct){var x=await service.UpdateAsync(id,r,ct);return x is null?NotFound():Ok(x);}
 [HttpGet("concepts")] public async Task<IActionResult> Concepts(CancellationToken ct)=>Ok(await service.GetConceptsAsync(ct));
 [HttpPost("{id:long}/suppliers"),RequirePermission(Permissions.QuotationsManage)] public async Task<IActionResult> AddSupplier(long id,CreateQuotationSupplierRequest r,CancellationToken ct){var x=await service.AddSupplierAsync(id,r,ct);return x is null?NotFound():Ok(x);}
 [HttpDelete("{id:long}/suppliers/{sid:long}"),RequirePermission(Permissions.QuotationsManage)] public async Task<IActionResult> DeleteSupplier(long id,long sid,CancellationToken ct)=>await service.DeleteSupplierAsync(id,sid,ct)?NoContent():NotFound();
 [HttpPost("{id:long}/cargo"),RequirePermission(Permissions.QuotationsManage)] public async Task<IActionResult> AddCargo(long id,CreateQuotationCargoItemRequest r,CancellationToken ct){var x=await service.AddCargoAsync(id,r,ct);return x is null?NotFound():Ok(x);}
 [HttpDelete("{id:long}/cargo/{cid:long}"),RequirePermission(Permissions.QuotationsManage)] public async Task<IActionResult> DeleteCargo(long id,long cid,CancellationToken ct)=>await service.DeleteCargoAsync(id,cid,ct)?NoContent():NotFound();
 [HttpPut("{id:long}/customs-taxes/ad-valorem"),RequirePermission(Permissions.QuotationsManage)] public async Task<IActionResult> UpdateAdValorem(long id,UpdateAdValoremRequest r,CancellationToken ct){var x=await service.UpdateAdValoremAsync(id,r,ct);return x is null?NotFound():Ok(x);}
 [HttpPost("{id:long}/customs-taxes"),RequirePermission(Permissions.QuotationsManage)] public async Task<IActionResult> AddTax(long id,CreateQuotationCustomsTaxRequest r,CancellationToken ct){var x=await service.AddTaxAsync(id,r,ct);return x is null?NotFound():Ok(x);}
 [HttpDelete("{id:long}/customs-taxes/{tid:long}"),RequirePermission(Permissions.QuotationsManage)] public async Task<IActionResult> DeleteTax(long id,long tid,CancellationToken ct)=>await service.DeleteTaxAsync(id,tid,ct)?NoContent():NotFound();
}
