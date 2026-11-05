import {api} from "@/lib/api/http";
const base=(id:number)=>`/api/finance/operations/${id}/customs-taxes`;
export type TaxItem={customsTaxItemId:number;taxCode:string;description:string;amountPen:number;documentReference:string|null;notes:string|null;paidPen:number;payerType:"COMPANY"|"CLIENT"|"UNREVIEWED";paymentDate:string|null};
export type TaxPayment={customsTaxPaymentId:number;customsTaxItemId:number;amountPen:number;paymentDate:string;payerType:string;fundingSource:string;paymentReference:string|null;notes:string|null};
export type TaxState={currency:"PEN";items:TaxItem[];payments:TaxPayment[];assessedPen:number;paidPen:number;advancesPen:number;refundsPen:number;advanceAppliedPen:number;unassignedPen:number;advanceBalancePen:number};
export type TaxInput={taxCode:string;description:string;amountPen:number;documentReference:string;notes:string;payerType:"COMPANY"|"CLIENT";paymentDate:string};
export const customsTaxesApi={
 get:(id:number)=>api.get<TaxState>(base(id)),
 add:(id:number,r:TaxInput)=>api.post(base(id),r),
 edit:(id:number,itemId:number,r:TaxInput)=>api.put(`${base(id)}/${itemId}`,r),
 void:(id:number,itemId:number,reason:string)=>api.post(`${base(id)}/${itemId}/void`,{reason}),
 pay:(id:number,itemId:number,r:{amountPen:number;paymentDate:string;payerType:string;fundingSource:string;paymentReference:string;notes:string})=>api.post(`${base(id)}/${itemId}/payments`,r),
 voidPayment:(id:number,paymentId:number,reason:string)=>api.post(`${base(id)}/payments/${paymentId}/void`,{reason})
};
