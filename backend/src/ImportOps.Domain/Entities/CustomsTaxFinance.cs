namespace ImportOps.Domain.Entities;
public sealed class CustomsTaxFinanceItem {
 public long CustomsTaxItemId {get;set;} public long CompanyId {get;set;} public long OperationId {get;set;}
 public string TaxCode {get;set;}=""; public string Description {get;set;}="";
 public string? DocumentReference {get;set;} public DateTime? AssessedAt {get;set;}
 public decimal AmountPen {get;set;} public string PayerType {get;set;}="UNREVIEWED"; public DateTime? PaymentDate {get;set;} public string? Notes {get;set;}
 public bool IsVoid {get;set;} public string? VoidReason {get;set;}
 public DateTime CreatedAt {get;set;} public long CreatedBy {get;set;}
 public DateTime? ModifiedAt {get;set;} public long? ModifiedBy {get;set;}
 public byte[] RowVer {get;set;}=Array.Empty<byte>();
}
public sealed class CustomsTaxFinancePayment {
 public long CustomsTaxPaymentId {get;set;} public long CompanyId {get;set;} public long OperationId {get;set;}
 public long CustomsTaxItemId {get;set;} public DateTime PaymentDate {get;set;}
 public decimal AmountPen {get;set;} public string PayerType {get;set;}=""; public string FundingSource {get;set;}="UNASSIGNED";
 public string? PaymentReference {get;set;} public string? Notes {get;set;}
 public bool IsVoid {get;set;} public string? VoidReason {get;set;}
 public DateTime CreatedAt {get;set;} public long CreatedBy {get;set;}
 public byte[] RowVer {get;set;}=Array.Empty<byte>();
}
