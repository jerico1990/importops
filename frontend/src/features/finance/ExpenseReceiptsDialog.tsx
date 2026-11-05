import {useEffect,useState,type FormEvent} from "react";
import {Dialog} from "@/components/ui/Dialog";
import {Button} from "@/components/ui/Button";
import {Input} from "@/components/ui/Input";
import {InlineAlert} from "@/components/feedback/InlineAlert";
import type {Expense} from "./types";
import {expenseReceiptsApi,type ExpenseReceipt} from "./finance.api";
import {formatMoney} from "@/lib/format";

export function ExpenseReceiptsDialog({operationId,expense,onClose,onChanged}:{operationId:number;expense:Expense|null;onClose:()=>void;onChanged:()=>void}){
 const [rows,setRows]=useState<ExpenseReceipt[]>([]),[loading,setLoading]=useState(false),[saving,setSaving]=useState(false),[error,setError]=useState("");
 const [file,setFile]=useState<File|null>(null),[date,setDate]=useState(""),[amount,setAmount]=useState(""),[reference,setReference]=useState("");
 const [voidId,setVoidId]=useState<number|null>(null),[reason,setReason]=useState("");
 const id=expense?.operationExpenseId;
 const refresh=async()=>{if(!id)return;setLoading(true);try{setRows(await expenseReceiptsApi.list(operationId,id));setError("")}catch(e){setError(e instanceof Error?e.message:"Error al consultar constancias")}finally{setLoading(false)}};
 useEffect(()=>{setRows([]);setFile(null);setDate("");setAmount("");setReference("");setVoidId(null);setError("");if(id)void refresh()},[id,operationId]);
 if(!expense)return null;
 const active=rows.filter(x=>!x.isVoid),documented=active.reduce((a,b)=>a+b.amount,0),pending=Math.max(0,expense.amount-documented);
 const currency=expense.currencyCode;
 const save=async(e:FormEvent)=>{e.preventDefault();if(!id||!file||!date||Number(amount)<=0||Number(amount)>pending){setError("Selecciona PDF, fecha e importe válido que no supere el pendiente.");return}setSaving(true);try{await expenseReceiptsApi.upload(operationId,id,{file,paymentDate:date,amount:Number(amount),referenceNo:reference});setFile(null);setDate("");setAmount("");setReference("");await refresh();onChanged()}catch(e){setError(e instanceof Error?e.message:"No se pudo adjuntar la constancia")}finally{setSaving(false)}};
 const download=async(r:ExpenseReceipt)=>{try{const result=await expenseReceiptsApi.download(operationId,id!,r.expensePaymentReceiptId);const url=URL.createObjectURL(result.blob);const a=document.createElement("a");a.href=url;a.download=result.fileName||r.originalFileName;document.body.appendChild(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),1000)}catch(e){setError(e instanceof Error?e.message:"No se pudo descargar")}};
 const toggleAnnex=async(r:ExpenseReceipt)=>{if(!id)return;setSaving(true);try{await expenseReceiptsApi.setClientAnnex(operationId,id,r.expensePaymentReceiptId,!r.includeInClientLiquidation);await refresh()}catch(e){setError(e instanceof Error?e.message:"No se pudo actualizar el permiso del anexo")}finally{setSaving(false)}};
 const voidReceipt=async()=>{if(!id||voidId===null||reason.trim().length<5)return;setSaving(true);try{await expenseReceiptsApi.void(operationId,id,voidId,reason.trim());setVoidId(null);setReason("");await refresh();onChanged()}catch(e){setError(e instanceof Error?e.message:"No se pudo anular")}finally{setSaving(false)}};
 return <Dialog open title="Constancias de pago" description={`${expense.description} · Solo uso interno R&B`} onClose={onClose} className="sm:max-w-2xl">
  <div className="space-y-4 text-sm">
   {error&&<InlineAlert message={error}/>}
   <div className="grid grid-cols-3 gap-2">{[["Gasto",expense.amount],["Documentado",documented],["Por documentar",pending]].map(([label,value])=><div key={String(label)} className="rounded-xl border border-slate-100 bg-slate-50/70 p-3"><div className="text-xs text-slate-500">{label}</div><div className="font-semibold">{formatMoney(Number(value),currency)}</div></div>)}</div>
   <div className="space-y-2"><h3 className="text-sm font-semibold text-slate-900">Archivos adjuntos ({active.length})</h3>
    {loading&&<p className="text-slate-500">Cargando...</p>}
    {!loading&&active.length===0&&<p className="rounded-lg bg-slate-50 p-3 text-slate-500">Este gasto todavía no tiene constancias.</p>}
    {active.map(r=><div key={r.expensePaymentReceiptId} className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-slate-200 p-3"><div><div className="font-medium break-all">{r.originalFileName}</div><div className="text-xs text-slate-500">{r.paymentDate.slice(0,10)} · {formatMoney(r.amount,r.currencyCode)} · {r.referenceNo||"Sin referencia"}</div></div><div className="flex flex-wrap items-center gap-2"><label className="flex items-center gap-1 text-xs text-slate-700"><input type="checkbox" checked={r.includeInClientLiquidation} disabled={saving} onChange={()=>void toggleAnnex(r)}/> Incluir en PDF del cliente</label><Button variant="secondary" onClick={()=>void download(r)}>Descargar</Button><Button variant="secondary" onClick={()=>{setVoidId(r.expensePaymentReceiptId);setReason("")}}>Anular</Button></div></div>)}
   </div>
   {voidId!==null&&<div className="space-y-2 rounded-lg border border-amber-200 p-3"><label className="block space-y-1 text-sm font-medium text-slate-700">Motivo de anulación (mínimo 5 caracteres)</label><Input value={reason} onChange={e=>setReason(e.target.value)}/><div className="flex justify-end gap-2"><Button variant="secondary" onClick={()=>setVoidId(null)}>Cancelar</Button><Button loading={saving} disabled={reason.trim().length<5} onClick={()=>void voidReceipt()}>Confirmar anulación</Button></div></div>}
   {pending>0&&<form className="space-y-4 border-t border-slate-100 pt-4" onSubmit={save}><h3 className="text-sm font-semibold text-slate-900">Adjuntar constancia</h3><div className="grid gap-4 sm:grid-cols-2"><label className="block space-y-1 text-sm font-medium text-slate-700">Fecha de pago *<Input type="date" value={date} max={new Date().toISOString().slice(0,10)} onChange={e=>setDate(e.target.value)}/></label><label className="block space-y-1 text-sm font-medium text-slate-700">Importe ({currency}) *<Input type="number" min="0.01" step="0.01" max={pending} value={amount} onChange={e=>setAmount(e.target.value)}/></label></div><label className="block space-y-1 text-sm font-medium text-slate-700">Referencia bancaria (opcional)<Input value={reference} onChange={e=>setReference(e.target.value)}/></label><label className="block space-y-1 text-sm font-medium text-slate-700">Constancia PDF *<Input type="file" accept="application/pdf,.pdf" onChange={e=>setFile(e.target.files?.[0]??null)}/></label><p className="text-xs text-slate-500">PDF hasta 15 MB. No genera movimientos ni importes adicionales en liquidación.</p><div className="flex justify-end pt-1"><Button type="submit" loading={saving} disabled={!file||!date||!(Number(amount)>0)}>Guardar constancia</Button></div></form>}
   <div className="flex justify-end border-t border-slate-100 pt-4"><Button variant="secondary" onClick={onClose}>Cerrar</Button></div>
  </div>
 </Dialog>;
}
