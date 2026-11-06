import{api,downloadFile}from"@/lib/api/http";import type{FinancialSummary,Budget,Fund,Expense,Service,Collection,Refund,Settlement}from"./types";
const base=(id:number)=>`/api/finance/operations/${id}`;
export type LiquidationLine = {payerType?:string; source:string;sourceId:number;description:string;reference:string|null;amount:number;currencyCode?:string };
export type LiquidationCurrency = {currencyCode:string;lines:LiquidationLine[];totalExpenses:number;totalServices:number;totalCustomsTaxes:number;totalDue:number;clientAdvances:number;serviceCollections:number;directClientTaxPayments:number;totalReceived:number;totalRefunded:number;balance:number};
export type Liquidation = {currencySections?:LiquidationCurrency[];operationId:number;operationNo:string;clientName:string;companyName:string;merchandise:string;currencyCode:string;lines:LiquidationLine[];totalExpenses:number;totalServices:number;totalDue:number;clientAdvances:number;serviceCollections:number;totalReceived:number;totalRefunded:number;balance:number;warnings:string[];number:string|null;issuedAt:string|null};
const liquidationBase=(id:number)=>`/api/operations/${id}/liquidation`;
export type FinancingInfo = { financingDays?:number|null; creditDays?:number|null; days?:number|null };
export type LiquidationDelivery = {
 sentAt?:string|null; sentVia?:string|null; sentTo?:string|null;
 dueAt?:string|null; dueDate?:string|null; financingDays?:number|null;
};
export const liquidationApi={
 financing:(id:number)=>api.get<FinancingInfo>(`${liquidationBase(id)}/financing`),
 updateFinancing:(id:number,financingDays:number)=>api.put<FinancingInfo>(`${liquidationBase(id)}/financing`,{financingDays}),
 delivery:(id:number)=>api.get<LiquidationDelivery>(`${liquidationBase(id)}/delivery`),
 registerSend:(id:number,r:{sentVia:string;sentTo:string;sentAt?:string})=>api.post<LiquidationDelivery>(`${liquidationBase(id)}/send`,r),
 preview:(id:number)=>api.get<Liquidation>(`${liquidationBase(id)}/preview`),
 issued:(id:number)=>api.get<Liquidation>(`${liquidationBase(id)}/issued`),
 issue:(id:number)=>api.post<Liquidation>(`${liquidationBase(id)}/issue`,{}),
 pdf:(id:number)=>downloadFile(`${liquidationBase(id)}/pdf`)
};
export type ExpensePaymentRecord={expensePaymentId:number;operationExpenseId:number;paymentDate:string;currencyCode:string;amount:number;baseAmount:number;payerType:string;paymentMethodId:number|null;referenceNo:string|null;notes:string|null;isVoid:boolean;rowVersion:string};
type AddFundRequest={requestedAmount?:number|null;receivedAmount:number;currencyCode:string;exchangeRate?:number|null;paymentMethodId?:number|null;referenceNo?:string|null;notes?:string|null};
type AddExpenseRequest={expenseCategoryId:number;supplierId?:number|null;description:string;documentReference?:string|null;expenseDate:string;dueDate?:string|null;currencyCode:string;amount:number;exchangeRate?:number|null;isRebillable:boolean;payerType?:"COMPANY"|"CLIENT";rebillAmount?:number|null;notes?:string|null};
type AddServiceRequest={serviceCategoryId:number;description:string;currencyCode:string;amount:number;exchangeRate?:number|null;notes?:string|null};
type AddCollectionRequest={collectionType:string;collectionDate:string;amount:number;currencyCode:string;exchangeRate?:number|null;paymentMethodId?:number|null;referenceNo?:string|null;notes?:string|null};
type AddRefundRequest={amount:number;currencyCode:string;exchangeRate?:number|null;paymentMethodId?:number|null;referenceNo?:string|null;reason:string;notes?:string|null};
export const financeApi={
 summary:(id:number)=>api.get<FinancialSummary>(`${base(id)}/summary`),
 budget:(id:number)=>api.get<Budget|null>(`${base(id)}/budget`),
 settlement:(id:number)=>api.get<Settlement|null>(`${base(id)}/settlement`),
 ensureBudget:(id:number)=>api.post<Budget>(`${base(id)}/budget/ensure`,{}),
 funds:(id:number)=>api.get<Fund[]>(`${base(id)}/funds`),
 addFund:(id:number,r:AddFundRequest)=>api.post<Fund>(`${base(id)}/funds`,r),
 expenses:(id:number)=>api.get<Expense[]>(`${base(id)}/expenses`),
 addExpense:(id:number,r:AddExpenseRequest)=>api.post<Expense>(`${base(id)}/expenses`,r),
 payExpense:(id:number,expenseId:number,r:{paymentDate:string;amount:number;currencyCode:string;exchangeRate?:number|null;payerType:string;paymentMethodId?:number|null;referenceNo?:string|null;notes?:string|null})=>api.post<Expense>(`${base(id)}/expenses/${expenseId}/payments`,r),
 expensePayments:(id:number,expenseId:number)=>api.get<ExpensePaymentRecord[]>(`${base(id)}/expenses/${expenseId}/payments`),
 voidExpensePayment:(id:number,expenseId:number,paymentId:number,rowVersion:string,reason:string)=>api.post<Expense>(`${base(id)}/expenses/${expenseId}/payments/${paymentId}/void`,{rowVersion,reason}),
 services:(id:number)=>api.get<Service[]>(`${base(id)}/services`),
 addService:(id:number,data:AddServiceRequest)=>api.post<Service>(`${base(id)}/services`,data),
 updateService:(id:number,itemId:number,rowVersion:string,data:AddServiceRequest)=>api.put<Service>(`${base(id)}/services/${itemId}`,{rowVersion,data}),
 voidService:(id:number,itemId:number,rowVersion:string,reason:string)=>api.post<void>(`${base(id)}/services/${itemId}/void`,{rowVersion,reason}),
 collections:(id:number)=>api.get<Collection[]>(`${base(id)}/collections`),
 addCollection:(id:number,r:AddCollectionRequest)=>api.post<Collection>(`${base(id)}/collections`,r),
 allocate:(id:number,collectionId:number,r:{operationServiceId:number;amount:number})=>api.post<void>(`${base(id)}/collections/${collectionId}/allocate`,r),
 adjustment:(id:number,r:{adjustmentType:string;reason:string;amount:number;currencyCode:string;exchangeRate?:number|null;adjustmentDate:string;notes?:string|null})=>api.post<void>(`${base(id)}/adjustments`,r),
 refunds:(id:number)=>api.get<Refund[]>(`${base(id)}/refunds`),
 addRefund:(id:number,r:AddRefundRequest)=>api.post<Refund>(`${base(id)}/refunds`,r),
 updateFund:(id:number,itemId:number,rowVersion:string,data:AddFundRequest)=>api.put<Fund>(`${base(id)}/funds/${itemId}`,{rowVersion,data}),
 voidFund:(id:number,itemId:number,rowVersion:string,reason:string)=>api.post<void>(`${base(id)}/funds/${itemId}/void`,{rowVersion,reason}),
 updateExpense:(id:number,itemId:number,rowVersion:string,data:AddExpenseRequest)=>api.put<Expense>(`${base(id)}/expenses/${itemId}`,{rowVersion,data}),
 voidExpense:(id:number,itemId:number,rowVersion:string,reason:string)=>api.post<void>(`${base(id)}/expenses/${itemId}/void`,{rowVersion,reason}),
 updateCollection:(id:number,itemId:number,rowVersion:string,data:AddCollectionRequest)=>api.put<Collection>(`${base(id)}/collections/${itemId}`,{rowVersion,data}),
 voidCollection:(id:number,itemId:number,rowVersion:string,reason:string)=>api.post<void>(`${base(id)}/collections/${itemId}/void`,{rowVersion,reason}),
 updateRefund:(id:number,itemId:number,rowVersion:string,data:AddRefundRequest)=>api.put<Refund>(`${base(id)}/refunds/${itemId}`,{rowVersion,data}),
 voidRefund:(id:number,itemId:number,rowVersion:string,reason:string)=>api.post<void>(`${base(id)}/refunds/${itemId}/void`,{rowVersion,reason}),
 prepareSettlement:(id:number)=>api.post<Settlement>(`${base(id)}/settlement/prepare`,{}),
 approveSettlement:(id:number,rowVersion:string,notes?:string)=>api.post<Settlement>(`${base(id)}/settlement/approve`,{rowVersion,notes:notes??null})
};

export type ExpenseReceipt = { expensePaymentReceiptId:number; operationExpenseId:number; paymentDate:string; amount:number; currencyCode:string; referenceNo:string|null; originalFileName:string; fileSizeBytes:number; uploadedAt:string; uploadedBy:number; isVoid:boolean; includeInClientLiquidation:boolean; voidReason:string|null };
export const expenseReceiptsApi = {
 setClientAnnex:(operationId:number,expenseId:number,receiptId:number,include:boolean)=>api.put<ExpenseReceipt>(`${base(operationId)}/expenses/${expenseId}/receipts/${receiptId}/client-annex`,{include}),
 list:(operationId:number,expenseId:number)=>api.get<ExpenseReceipt[]>(`${base(operationId)}/expenses/${expenseId}/receipts`),
 upload:(operationId:number,expenseId:number,data:{file:File;paymentDate:string;amount:number;referenceNo?:string})=>{
  const form=new FormData();form.append("file",data.file);form.append("paymentDate",data.paymentDate);form.append("amount",String(data.amount));if(data.referenceNo)form.append("referenceNo",data.referenceNo);
  return api.post<ExpenseReceipt>(`${base(operationId)}/expenses/${expenseId}/receipts`,form);
 },
 download:(operationId:number,expenseId:number,receiptId:number)=>downloadFile(`${base(operationId)}/expenses/${expenseId}/receipts/${receiptId}/download`),
 void:(operationId:number,expenseId:number,receiptId:number,reason:string)=>api.post<ExpenseReceipt>(`${base(operationId)}/expenses/${expenseId}/receipts/${receiptId}/void`,{reason})
};
