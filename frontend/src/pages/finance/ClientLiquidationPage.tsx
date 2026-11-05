import {useEffect,useState} from 'react';
import {useParams,Link} from 'react-router-dom';
import {useAuth} from '@/features/auth/AuthContext';
import {liquidationApi,type Liquidation} from '@/features/finance/finance.api';
import {formatMoney} from '@/lib/format';
import {Button} from '@/components/ui/Button';

/** Only issued data is loaded. Never request financial movements, drafts or internal attachments. */
export function ClientLiquidationPage(){
 const {id}=useParams();const operationId=Number(id);const {user}=useAuth();
 const [data,setData]=useState<Liquidation|null>(null),[busy,setBusy]=useState(false),[error,setError]=useState(''),[loading,setLoading]=useState(true);
 useEffect(()=>{if(user?.userType!=='CLIENT'||!Number.isSafeInteger(operationId)||operationId<=0){setLoading(false);return;}
  setLoading(true);liquidationApi.issued(operationId).then(setData).catch(e=>setError(e instanceof Error?e.message:'Liquidación no disponible')).finally(()=>setLoading(false));
 },[operationId,user?.userType]);
 const download=async()=>{setBusy(true);setError('');try{const file=await liquidationApi.pdf(operationId);const url=URL.createObjectURL(file.blob);const a=document.createElement('a');a.href=url;a.download=file.fileName||'Liquidacion-final.pdf';document.body.appendChild(a);a.click();a.remove();setTimeout(()=>URL.revokeObjectURL(url),1000)}catch(e){setError(e instanceof Error?e.message:'No se pudo descargar el PDF')}finally{setBusy(false)}};
 if(user?.userType!=='CLIENT')return <p className="p-4 text-sm text-slate-600">Vista exclusiva del importador. <Link to="/operaciones" className="underline">Volver</Link></p>;
 if(loading)return <p className="p-4 text-sm text-slate-500">Consultando liquidación emitida...</p>;
 if(!data)return <div className="rounded-xl border bg-white p-5 text-sm text-slate-600">{error||'La liquidación aún no ha sido emitida.'}</div>;
 const sections=data.currencySections?.length?data.currencySections:[];
 return <div className="mx-auto max-w-6xl space-y-4 rounded-xl border border-slate-200 bg-white p-5 md:p-7">
  <header className="flex flex-wrap items-start justify-between gap-4 border-b pb-4"><div><p className="text-xs uppercase text-slate-500">{data.companyName}</p><h1 className="text-xl font-bold text-slate-900">LIQUIDACIÓN FINAL DE IMPORTACIÓN</h1><p className="text-sm text-slate-500">N.º {data.number} · Emitida {data.issuedAt?new Date(data.issuedAt).toLocaleDateString('es-PE'):''}</p></div><Button onClick={()=>void download()} loading={busy}>Descargar liquidación y comprobantes (PDF)</Button></header>
  <div className="grid gap-2 text-sm sm:grid-cols-2"><p><b>Cliente:</b> {data.clientName}</p><p><b>Operación:</b> {data.operationNo}</p><p className="sm:col-span-2"><b>Mercancía:</b> {data.merchandise||'—'}</p></div>
  {error&&<p role="alert" className="rounded-lg bg-red-50 p-3 text-sm text-red-700">{error}</p>}
  {sections.map(s=><section key={s.currencyCode} className="space-y-3 rounded-xl border p-4"><h2 className="font-semibold">Liquidación en {s.currencyCode==='PEN'?'soles (PEN)':'dólares (USD)'}</h2><div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="border-b text-left"><th className="py-2">Concepto / referencia</th><th>Desembolso</th><th className="text-right">Importe</th></tr></thead><tbody>{s.lines.map(l=><tr key={`${l.source}-${l.sourceId}`} className="border-b"><td className="py-2">{l.description}{l.reference&&<small className="block text-slate-500">{l.reference}</small>}</td><td>{l.payerType==='CLIENT'?'Importador':l.payerType==='COMPANY'?'R&B':'Por revisar'}</td><td className="text-right tabular-nums">{formatMoney(l.amount,s.currencyCode)}</td></tr>)}</tbody></table></div><div className="ml-auto max-w-lg space-y-2 text-sm">{([['Gastos de terceros',s.totalExpenses],['Servicios R&B',s.totalServices],...(s.currencyCode==='PEN'?[['Tributos aduaneros',s.totalCustomsTaxes] as const]:[]),['Total liquidado',s.totalDue],['Anticipos recibidos',s.clientAdvances],['Cobros por servicios',s.serviceCollections],['Desembolsos directos del importador',s.directClientTaxPayments],['Devoluciones',s.totalRefunded]] as readonly (readonly [string,number])[]).map(([label,n])=><div key={label} className="flex justify-between gap-3"><span>{label}</span><b>{formatMoney(n,s.currencyCode)}</b></div>)}<div className="flex justify-between gap-3 border-t pt-3 font-bold"><span>{s.balance>0?'Saldo a favor del cliente':s.balance<0?'Saldo por cobrar':'Saldo conciliado'}</span><span>{formatMoney(Math.abs(s.balance),s.currencyCode)}</span></div></div></section>)}
 </div>;
}
