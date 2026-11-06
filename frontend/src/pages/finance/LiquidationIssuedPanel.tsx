import {useEffect,useState} from 'react';
import {liquidationApi,type Liquidation,type FinancingInfo,type LiquidationDelivery} from '@/features/finance/finance.api';
import {formatMoney} from '@/lib/format';
import {Button} from '@/components/ui/Button';
import {ActionDialog,type ActionRequest} from '@/components/ui/ActionDialog';

export function LiquidationIssuedPanel({operationId,canApprove,view="document",revision=0}:{operationId:number;canApprove:boolean;view?:"document"|"financing";revision?:number}) {
 const [data,setData]=useState<Liquidation|null>(null);
 const [actionRequest,setActionRequest]=useState<ActionRequest|null>(null);
 const [issued,setIssued]=useState(false);
 const [busy,setBusy]=useState(false);
 const [financing,setFinancing]=useState<FinancingInfo|null>(null);
 const [delivery,setDelivery]=useState<LiquidationDelivery|null>(null);
 const [days,setDays]=useState('30');
 const [sentVia,setSentVia]=useState('EMAIL');
 const [sentTo,setSentTo]=useState('');
 const [sentAt,setSentAt]=useState('');
 const [financeBusy,setFinanceBusy]=useState(false);
 const [financeError,setFinanceError]=useState('');
 const [financeNotice,setFinanceNotice]=useState('');
 const refreshFinancing=async()=>{
  const f=await liquidationApi.financing(operationId);
  let d:LiquidationDelivery|null=null;
  try{d=await liquidationApi.delivery(operationId)}catch(e){if(!(e instanceof Error && /\b404\b|not found/i.test(e.message)))throw e;}
  setFinancing(f);setDelivery(d);
  setDays(String(f.financingDays??f.creditDays??f.days??30));
 };
 const saveDays=async()=>{
  const n=Number(days);
  if(!Number.isInteger(n)||n<=0||n>365){setFinanceError('Indica un plazo válido entre 1 y 365 días.');return;}
  setFinanceBusy(true);setFinanceError('');setFinanceNotice('');
  try{await liquidationApi.updateFinancing(operationId,n);await refreshFinancing();setFinanceNotice('Plazo guardado.');}
  catch(e){setFinanceError(e instanceof Error?e.message:'No se pudo guardar el plazo.');}
  finally{setFinanceBusy(false);}
 };
 const registerSend=async()=>{
  if(!sentTo.trim()){setFinanceError('Indica el destinatario del envío.');return;}

  setFinanceBusy(true);setFinanceError('');setFinanceNotice('');
  try{await liquidationApi.registerSend(operationId,{sentVia,sentTo:sentTo.trim(),...(sentAt?{sentAt:new Date(sentAt).toISOString()}:{})});await refreshFinancing();setFinanceNotice('Envío registrado correctamente.');}
  catch(e){setFinanceError(e instanceof Error?e.message:'No se pudo registrar el envío.');}
  finally{setFinanceBusy(false);}
 };
 const [error,setError]=useState('');
 const refresh=async()=>{
  try {const x=await liquidationApi.issued(operationId);setData(x);setIssued(true)}
  catch {setIssued(false);const x=await liquidationApi.preview(operationId);setData(x)}
 };
 useEffect(()=>{
  void refresh().catch(e=>setError(e instanceof Error?e.message:'No se pudo consultar la liquidación'));
  void refreshFinancing().catch(e=>setFinanceError(e instanceof Error?e.message:'No se pudo consultar el financiamiento'));
 },[operationId,revision]);
 const issue=async()=>{setBusy(true);setError('');try{const x=await liquidationApi.issue(operationId);setData(x);setIssued(true);void refreshFinancing().catch(()=>{})}catch(e){setError(e instanceof Error?e.message:'No se pudo emitir')}finally{setBusy(false)}};
 const pdf=async()=>{setBusy(true);setError('');try{const x=await liquidationApi.pdf(operationId);const url=URL.createObjectURL(x.blob);const a=document.createElement('a');a.href=url;a.download=x.fileName;document.body.appendChild(a);a.click();a.remove();URL.revokeObjectURL(url)}catch(e){setError(e instanceof Error?e.message:'No se pudo descargar')}finally{setBusy(false)}};
 if(!data)return <p className="text-sm text-slate-500">{error||'Cargando liquidación...'}</p>;
 const money=(n:number)=>formatMoney(n,data.currencyCode);
 const financingSection = <>
  <section className="space-y-4 rounded-xl border border-slate-200 bg-slate-50 p-4">
   <div><h3 className="font-bold">Financiamiento y envío al importador</h3><p className="text-sm text-slate-600">El plazo comienza cuando se registra el envío, no cuando se emite la liquidación. Registrar el envío no envía un correo automáticamente.</p></div>
   {financeError&&<p className="rounded bg-red-50 p-3 text-sm text-red-800">{financeError}</p>}
   {financeNotice&&<p className="rounded bg-green-50 p-3 text-sm text-green-800">{financeNotice}</p>}
   <div className="flex flex-wrap items-end gap-3">
    <label className="text-sm">Plazo acordado (días calendario)<input className="mt-1 block w-36 rounded-lg border bg-white p-2" type="number" min="1" max="365" value={days} disabled={!canApprove||Boolean(delivery?.sentAt)} onChange={e=>setDays(e.target.value)}/></label>
    {canApprove&&!delivery?.sentAt&&<Button variant="secondary" disabled={financeBusy||!financing} onClick={()=>void saveDays()}>Guardar plazo</Button>}
   </div>
   {delivery?.sentAt?<div className="grid gap-2 text-sm sm:grid-cols-2"><p><b>Enviada:</b> {new Date(delivery.sentAt).toLocaleString('es-PE')}</p><p><b>Medio:</b> {delivery.sentVia||'—'}</p><p><b>Destinatario:</b> {delivery.sentTo||'—'}</p><p><b>Vencimiento:</b> {delivery.dueAt||delivery.dueDate?new Date((delivery.dueAt||delivery.dueDate)!).toLocaleDateString('es-PE'):'No disponible'}</p></div>
   :issued?<div className="space-y-3"><p className="text-sm font-medium">Registrar envío realizado</p><div className="grid gap-3 sm:grid-cols-2"><label className="text-sm">Medio<select className="mt-1 block w-full rounded-lg border bg-white p-2" value={sentVia} onChange={e=>setSentVia(e.target.value)}><option value="EMAIL">Correo electrónico</option><option value="MANUAL">Entrega manual</option><option value="OTHER">Otro</option></select></label><label className="text-sm">Destinatario<input className="mt-1 block w-full rounded-lg border bg-white p-2" value={sentTo} onChange={e=>setSentTo(e.target.value)} placeholder="Correo o nombre del destinatario"/></label><label className="text-sm">Fecha y hora de envío (opcional)<input className="mt-1 block w-full rounded-lg border bg-white p-2" type="datetime-local" value={sentAt} onChange={e=>setSentAt(e.target.value)}/></label></div>{canApprove&&<Button disabled={financeBusy||!financing} onClick={()=>setActionRequest({title:"Confirmar envío",description:"Confirma que ya enviaste la liquidación al importador. Esta acción inicia el plazo de financiamiento.",onConfirm:registerSend})}>Registrar envío de liquidación</Button>}</div>
   :<p className="text-sm text-slate-500">Primero emite la liquidación para habilitar el registro de envío.</p>}
  </section>
 <ActionDialog request={actionRequest} onClose={()=>setActionRequest(null)}/></>;
 if(view==="financing")return financingSection;
 return <div className="space-y-4 rounded-xl border bg-white p-5 md:p-8">
  <div className="flex flex-wrap items-start justify-between gap-4 border-b pb-4"><div><p className="text-xs uppercase text-slate-500">{data.companyName}</p><h2 className="text-xl font-bold">LIQUIDACIÓN FINAL DE IMPORTACIÓN</h2><p className="text-sm text-slate-500">{issued?`N.º ${data.number} · Emitida ${new Date(data.issuedAt!).toLocaleDateString('es-PE')}`:'BORRADOR — SIN EMITIR'}</p></div><div className="flex gap-2">{issued?<Button onClick={()=>void pdf()} loading={busy}>Descargar PDF</Button>:canApprove?<Button onClick={()=>setActionRequest({title:"Emitir liquidación",description:"La emisión congela los importes definitivos. No podrás editar los conceptos sin una rectificación formal.",onConfirm:issue})} disabled={data.warnings.length>0||!(data.currencySections?.some(section=>section.lines.length>0)??data.lines.length>0)} loading={busy}>Emitir liquidación</Button>:null}</div></div>
  <div className="grid gap-2 text-sm md:grid-cols-2"><p><b>Cliente:</b> {data.clientName}</p><p><b>Operación:</b> {data.operationNo}</p><p className="md:col-span-2"><b>Mercancía:</b> {data.merchandise||'—'}</p></div>
  {error&&<p className="rounded bg-red-50 p-3 text-sm text-red-800">{error}</p>}
  {data.warnings.length>0&&<div className="rounded bg-amber-50 p-3 text-sm text-amber-900"><b>Revisar antes de emitir:</b><ul className="list-disc pl-5">{data.warnings.map((w,i)=><li key={i}>{w}</li>)}</ul></div>}
  {(data.currencySections?.length ? data.currencySections : [{currencyCode:data.currencyCode,lines:data.lines,totalDue:data.totalDue,totalExpenses:data.totalExpenses,totalServices:data.totalServices,totalCustomsTaxes:0,clientAdvances:data.clientAdvances,serviceCollections:data.serviceCollections,directClientTaxPayments:0,totalReceived:data.totalReceived,totalRefunded:data.totalRefunded,balance:data.balance}]).map(section => {
   const fmt=(n:number)=>formatMoney(n,section.currencyCode);
   return <section key={section.currencyCode} className="space-y-3 rounded-xl border border-slate-200 p-4">
    <h3 className="font-bold">Liquidación en {section.currencyCode==='PEN'?'soles (PEN)':'dólares (USD)'}</h3>
    <div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="border-b text-left"><th className="py-2">Concepto / referencia</th><th className="text-left">Desembolso</th><th className="text-right">Importe</th></tr></thead><tbody>{section.lines.map(x=><tr className="border-b" key={`${x.source}-${x.sourceId}`}><td className="py-2">{x.description}{x.reference&&<span className="block text-xs text-slate-500">{x.reference}</span>}</td><td className="text-xs text-slate-600">{x.payerType==="CLIENT"?"Importador":x.payerType==="COMPANY"?"R&B":"Por revisar"}</td><td className="text-right tabular-nums">{fmt(x.amount)}</td></tr>)}</tbody></table></div>
    {!section.lines.length&&<p className="text-sm text-slate-500">Sin conceptos registrados en esta moneda.</p>}
    <div className="ml-auto max-w-lg space-y-2 text-sm">{([['Gastos de terceros',section.totalExpenses],['Servicios R&B',section.totalServices],...(section.currencyCode==='PEN'?[['Tributos aduaneros',section.totalCustomsTaxes] as const]:[]),['Total liquidado',section.totalDue],['Anticipos recibidos',section.clientAdvances],['Cobros por servicios',section.serviceCollections],['Desembolsos directos del importador',section.directClientTaxPayments],['Devoluciones',section.totalRefunded]] as readonly (readonly [string,number])[]).map(([label,value])=><div className="flex justify-between gap-3" key={label}><span>{label}</span><b className="tabular-nums">{fmt(value)}</b></div>)}<div className="flex justify-between border-t pt-3 font-bold"><span>{section.balance>0?'Saldo a favor del cliente':section.balance<0?'Saldo por cobrar':'Saldo conciliado'}</span><span>{fmt(Math.abs(section.balance))}</span></div></div>
   </section>;
  })}
  <ActionDialog request={actionRequest} onClose={()=>setActionRequest(null)}/>
  <p className="text-xs text-slate-500">La emisión congela los importes del documento. No aprueba ni cierra automáticamente la rendición financiera.</p>
 </div>;
}
