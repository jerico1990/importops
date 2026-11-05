import {quotationsApi} from "@/features/quotations/quotations.api";import type {QuotationConcept} from "@/features/quotations/types";import {useEffect,useState,type FormEvent} from "react";
import {Dialog} from "@/components/ui/Dialog";
import {Button} from "@/components/ui/Button";
import {Input} from "@/components/ui/Input";
import {MoneyInput} from "@/components/ui/MoneyInput";
import {SearchableSelect} from "@/components/ui/SearchableSelect";
import {InlineAlert} from "@/components/feedback/InlineAlert";
import {catalogsApi} from "@/features/catalogs/catalogs.api";
import type {ServiceCategory} from "@/features/catalogs/types";
import {financeApi} from "./finance.api";
import type {Service} from "./types";
export function ServiceEntryDialog({open,operationId,editing,onClose,onSaved}:{open:boolean;operationId:number;editing:Service|null;onClose:()=>void;onSaved:()=>Promise<void>|void}){
 const [concepts,setConcepts]=useState<QuotationConcept[]>([]);const [selectedConcept,setSelectedConcept]=useState<string>();const [categories,setCategories]=useState<ServiceCategory[]>([]);
 const [category,setCategory]=useState<string>();const [description,setDescription]=useState("");const [currency,setCurrency]=useState("USD");const [amount,setAmount]=useState("");const [rate,setRate]=useState("");const [notes,setNotes]=useState("");const [error,setError]=useState("");const [busy,setBusy]=useState(false);
 useEffect(()=>{if(!open)return;setSelectedConcept(undefined);void quotationsApi.concepts().then(setConcepts).catch(()=>setConcepts([]));setCategory(editing?.serviceCategoryId?String(editing.serviceCategoryId):undefined);setDescription(editing?.description||"");setCurrency(editing?.currencyCode||"USD");setAmount(editing?String(editing.amount):"");setRate(editing?.exchangeRate?String(editing.exchangeRate):"");setNotes(editing?.notes||"");setError("");void catalogsApi.serviceCategories().then(setCategories).catch(()=>setError("No se pudieron cargar las categorías."));},[open,editing]);
 const save=async(e:FormEvent)=>{e.preventDefault();if(!Number(category)||!description.trim()||!(Number(amount)>0)){setError("Completa categoría, descripción e importe válido.");return;}setBusy(true);setError("");try{const data={serviceCategoryId:Number(category),description:description.trim(),currencyCode:currency,amount:Number(amount),exchangeRate:rate?Number(rate):null,notes:notes||null};if(editing)await financeApi.updateService(operationId,editing.operationServiceId,editing.rowVersion,data);else await financeApi.addService(operationId,data);await onSaved();}catch(e){setError(e instanceof Error?e.message:"No se pudo guardar el servicio.");}finally{setBusy(false)}};
 return <Dialog open={open} onClose={onClose} title={editing?"Editar servicio propio R&B":"Registrar servicio propio R&B"} className="sm:max-w-2xl">
  <form className="space-y-4" onSubmit={save}>
   {error&&<InlineAlert message={error}/>}
   <div className="grid gap-3 sm:grid-cols-2">
    <div className="space-y-1"><label className="text-sm font-medium text-slate-700">Categoría *</label><SearchableSelect value={category} onChange={v=>{setCategory(v);setSelectedConcept(undefined)}} options={categories.map(c=>({value:String(c.serviceCategoryId),label:c.name}))} placeholder="Seleccionar categoría"/></div>
    <div className="space-y-1"><label className="text-sm font-medium text-slate-700">Moneda *</label><SearchableSelect value={currency} onChange={v=>setCurrency(v||"USD")} options={[{value:"USD",label:"USD — Dólar"},{value:"PEN",label:"PEN — Sol"}]}/></div>
   </div>
   <div className="space-y-1"><label className="text-sm font-medium text-slate-700">Concepto del catálogo (opcional)</label><SearchableSelect value={selectedConcept} onChange={v=>{setSelectedConcept(v);const c=concepts.find(x=>String(x.quotationConceptId)===v);if(c)setDescription(c.name)}} options={concepts.filter(x=>x.serviceCategoryId===Number(category)).map(x=>({value:String(x.quotationConceptId),label:x.name,description:x.code}))} placeholder="Seleccionar concepto"/></div>
   <div className="space-y-1"><label className="text-sm font-medium text-slate-700">Descripción del servicio *</label><Input value={description} onChange={e=>setDescription(e.target.value)} placeholder="Servicio realmente prestado"/></div>
   <div className="grid gap-3 sm:grid-cols-2">
    <div className="space-y-1"><label className="text-sm font-medium text-slate-700">Importe *</label><MoneyInput min="0" value={amount} onValueChange={setAmount} placeholder="0.00"/></div>
    <div className="space-y-1"><label className="text-sm font-medium text-slate-700">Tipo de cambio (si aplica)</label><Input type="number" min="0" step="0.0001" value={rate} onChange={e=>setRate(e.target.value)} placeholder="Tipo de cambio"/></div>
   </div>
   <div className="space-y-1"><label className="text-sm font-medium text-slate-700">Notas adicionales (opcional)</label><textarea className="focus-ring min-h-20 w-full rounded-xl border border-slate-200 px-3 py-2.5 text-sm" value={notes} onChange={e=>setNotes(e.target.value)} placeholder="Observaciones del servicio"/></div>
   <div className="flex justify-end gap-2 border-t border-slate-100 pt-4"><Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button><Button type="submit" loading={busy}>{editing?"Guardar cambios":"Registrar servicio"}</Button></div>
  </form>
 </Dialog>;
}
