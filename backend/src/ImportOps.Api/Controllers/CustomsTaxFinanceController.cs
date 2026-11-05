using ImportOps.Api.Security;
using ImportOps.Application.Common;
using ImportOps.Application.Security;
using ImportOps.Domain.Entities;
using ImportOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportOps.Api.Controllers;

public sealed record TaxItemInput(string TaxCode,string Description,decimal AmountPen,string? DocumentReference,string? Notes,string PayerType="COMPANY",DateTime? PaymentDate=null);
public sealed record TaxPaymentInput(decimal AmountPen,DateTime PaymentDate,string PayerType,string FundingSource,string? PaymentReference,string? Notes);
public sealed record TaxVoidInput(string Reason);

[ApiController]
[Route("api/finance/operations/{operationId:long}/customs-taxes")]
[RequirePermission(Permissions.FinanceView)]
public sealed class CustomsTaxFinanceController(ImportOpsDbContext db, ICurrentCompany company, ICurrentUser user):ControllerBase {
 private async Task<bool> Exists(long id,CancellationToken ct)=>await db.ImportOperations.AsNoTracking().AnyAsync(x=>x.OperationId==id&&x.CompanyId==company.CompanyId,ct);
 private async Task<bool> Frozen(long id,CancellationToken ct)=>await db.IssuedLiquidations.AsNoTracking().AnyAsync(x=>x.OperationId==id&&x.CompanyId==company.CompanyId,ct) || await db.OperationSettlements.AsNoTracking().AnyAsync(x=>x.OperationId==id&&x.CompanyId==company.CompanyId&&x.SettlementStatus=="APPROVED",ct);
 private static bool ValidCode(string? code)=>new[]{"ADVALOREM","IGV","IPM","PERCEPCION","OTRO"}.Contains(code?.Trim().ToUpperInvariant());
 [HttpGet] public async Task<IActionResult> Get(long operationId,CancellationToken ct) {
  if(!await Exists(operationId,ct))return NotFound();
  var items=await db.CustomsTaxFinanceItems.AsNoTracking().Where(x=>x.CompanyId==company.CompanyId&&x.OperationId==operationId&&!x.IsVoid).OrderBy(x=>x.CustomsTaxItemId).ToListAsync(ct);
  var payments=await db.CustomsTaxFinancePayments.AsNoTracking().Where(x=>x.CompanyId==company.CompanyId&&x.OperationId==operationId&&!x.IsVoid).ToListAsync(ct);
  var advancesPen=await db.ClientFunds.AsNoTracking().Where(x=>x.CompanyId==company.CompanyId&&x.OperationId==operationId&&!x.IsDeleted&&x.FundStatus!="CANCELLED"&&x.CurrencyCode=="PEN").SumAsync(x=>(decimal?)x.ReceivedAmount,ct)??0;
  var refundsPen=await db.ClientRefunds.AsNoTracking().Where(x=>x.CompanyId==company.CompanyId&&x.OperationId==operationId&&x.RefundStatus=="COMPLETED"&&x.CurrencyCode=="PEN").SumAsync(x=>(decimal?)x.Amount,ct)??0;
  var advanceAppliedPen=payments.Where(x=>x.FundingSource=="CLIENT_ADVANCE").Sum(x=>x.AmountPen);
  var unassignedPen=payments.Where(x=>x.FundingSource=="UNASSIGNED").Sum(x=>x.AmountPen);
  return Ok(new {currency="PEN",advancesPen,refundsPen,advanceAppliedPen,unassignedPen,advanceBalancePen=advancesPen-refundsPen-advanceAppliedPen,items=items.Select(x=>new {x.CustomsTaxItemId,x.TaxCode,x.Description,x.AmountPen,x.DocumentReference,x.Notes,x.PayerType,x.PaymentDate,paidPen=x.AmountPen}), payments=payments.Select(x=>new {x.CustomsTaxPaymentId,x.CustomsTaxItemId,x.AmountPen,x.PaymentDate,x.PayerType,x.FundingSource,x.PaymentReference,x.Notes}),assessedPen=items.Sum(x=>x.AmountPen),paidPen=items.Sum(x=>x.AmountPen)});
 }
 [HttpPost] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> Add(long operationId,TaxItemInput r,CancellationToken ct){
  if(!await Exists(operationId,ct))return NotFound(); if(await Frozen(operationId,ct))return Conflict("La liquidación está aprobada o emitida.");
  if(!ValidCode(r.TaxCode)||r.AmountPen<=0||r.PaymentDate is null||string.IsNullOrWhiteSpace(r.Description)||!new[]{"COMPANY","CLIENT"}.Contains(r.PayerType))return BadRequest("Código, descripción o importe inválidos.");
  var x=new CustomsTaxFinanceItem{CompanyId=company.CompanyId,OperationId=operationId,TaxCode=r.TaxCode.Trim().ToUpperInvariant(),Description=r.Description.Trim(),AmountPen=r.AmountPen,PayerType=r.PayerType,PaymentDate=r.PaymentDate,DocumentReference=r.DocumentReference,Notes=r.Notes,CreatedAt=DateTime.UtcNow,CreatedBy=user.UserId};
  db.CustomsTaxFinanceItems.Add(x);await db.SaveChangesAsync(ct);return Ok(new{x.CustomsTaxItemId});
 }
 [HttpPut("{itemId:long}")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> Edit(long operationId,long itemId,TaxItemInput r,CancellationToken ct){
  if(await Frozen(operationId,ct))return Conflict("La liquidación está aprobada o emitida.");
  var x=await db.CustomsTaxFinanceItems.SingleOrDefaultAsync(x=>x.CompanyId==company.CompanyId&&x.OperationId==operationId&&x.CustomsTaxItemId==itemId&&!x.IsVoid,ct);if(x is null)return NotFound();
  if(!ValidCode(r.TaxCode)||r.AmountPen<=0||r.PaymentDate is null||string.IsNullOrWhiteSpace(r.Description)||!new[]{"COMPANY","CLIENT"}.Contains(r.PayerType))return BadRequest("Datos inválidos.");
  var paid=await db.CustomsTaxFinancePayments.Where(p=>p.CustomsTaxItemId==itemId&&!p.IsVoid).SumAsync(p=>(decimal?)p.AmountPen,ct)??0;
  // Los pagos históricos no determinan el importe del tributo registrado.
  x.TaxCode=r.TaxCode.Trim().ToUpperInvariant();x.Description=r.Description.Trim();x.AmountPen=r.AmountPen;x.PayerType=r.PayerType;x.PaymentDate=r.PaymentDate;x.DocumentReference=r.DocumentReference;x.Notes=r.Notes;x.ModifiedAt=DateTime.UtcNow;x.ModifiedBy=user.UserId;
  await db.SaveChangesAsync(ct);return NoContent();
 }
 [HttpPost("{itemId:long}/void")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> Void(long operationId,long itemId,TaxVoidInput r,CancellationToken ct){
  if(await Frozen(operationId,ct))return Conflict("La liquidación está aprobada o emitida.");if(string.IsNullOrWhiteSpace(r.Reason))return BadRequest("Motivo obligatorio.");
  var x=await db.CustomsTaxFinanceItems.SingleOrDefaultAsync(x=>x.CompanyId==company.CompanyId&&x.OperationId==operationId&&x.CustomsTaxItemId==itemId&&!x.IsVoid,ct);if(x is null)return NotFound();
  // Los pagos históricos no condicionan la anulación del tributo en el nuevo flujo.
  x.IsVoid=true;x.VoidReason=r.Reason.Trim();x.ModifiedAt=DateTime.UtcNow;x.ModifiedBy=user.UserId;await db.SaveChangesAsync(ct);return NoContent();
 }
 // Ruta histórica conservada para compatibilidad, pero deshabilitada: evita registrar el mismo tributo dos veces.
 [HttpPost("{itemId:long}/payments")]
 [RequirePermission(Permissions.FinanceManage)]
 public IActionResult Pay(long operationId,long itemId,TaxPaymentInput r) => Conflict("Los tributos se registran como pagados directamente. Edite el tributo para corregirlo.");
 [HttpPost("payments/{paymentId:long}/void")] [RequirePermission(Permissions.FinanceManage)] public async Task<IActionResult> VoidPayment(long operationId,long paymentId,TaxVoidInput r,CancellationToken ct){
  if(await Frozen(operationId,ct))return Conflict("La liquidación está aprobada o emitida.");if(string.IsNullOrWhiteSpace(r.Reason))return BadRequest("Motivo obligatorio.");
  var x=await db.CustomsTaxFinancePayments.SingleOrDefaultAsync(x=>x.CompanyId==company.CompanyId&&x.OperationId==operationId&&x.CustomsTaxPaymentId==paymentId&&!x.IsVoid,ct);if(x is null)return NotFound();
  x.IsVoid=true;x.VoidReason=r.Reason.Trim();await db.SaveChangesAsync(ct);return NoContent();
 }
}
