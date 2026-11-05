import { useCallback, useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Pencil, Plus, RefreshCcw, Trash2, CheckCircle2 } from "lucide-react";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { Tabs } from "@/components/ui/Tabs";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { Skeleton } from "@/components/ui/Skeleton";
import { useAuth } from "@/features/auth/AuthContext";
import { Permissions } from "@/types/auth";
import { operationsApi } from "@/features/operations/operations.api";
import type { OperationDetail } from "@/features/operations/types";
import { financeApi, liquidationApi, expenseReceiptsApi, type ExpensePaymentRecord, type ExpenseReceipt } from "@/features/finance/finance.api";
import type { FinancialSummary, Budget, Settlement, Fund, Expense, Service, Collection, Refund } from "@/features/finance/types";
import { FinanceEntryDialog, type FinanceEntryKind } from "@/features/finance/FinanceEntryDialog";
import { LiquidationIssuedPanel } from "./LiquidationIssuedPanel";
import { Dialog } from "@/components/ui/Dialog";
import { ActionDialog, type ActionRequest } from "@/components/ui/ActionDialog";
import { CustomsTaxesPanel } from "@/features/finance/CustomsTaxesPanel";
import { customsTaxesApi, type TaxItem } from "@/features/finance/customsTaxes.api";
import { ServiceEntryDialog } from "@/features/finance/ServiceEntryDialog";
import { formatMoney } from "@/lib/format";
import {ExpenseReceiptsDialog} from "@/features/finance/ExpenseReceiptsDialog";

type Tab = "COSTS" | "MOVEMENTS" | "CUSTOMS" | "FINANCING" | "LIQUIDATION";
type Editable = Fund | Expense | Collection | Refund;
const isExpense = (x: Editable): x is Expense => "operationExpenseId" in x;
const isFund = (x: Editable): x is Fund => "clientFundId" in x;
const isCollection = (x: Editable): x is Collection => "clientCollectionId" in x;
const isRefund = (x: Editable): x is Refund => "clientRefundId" in x;

export function FinanceDetailPage() {
  const { id } = useParams(); const operationId = Number(id); const navigate = useNavigate();
  const { hasPermission, user } = useAuth();
  const canManage = user?.userType === "INTERNAL" && hasPermission(Permissions.FinanceManage);
  const canApprove = hasPermission(Permissions.FinanceApprove);
  const [tab, setTab] = useState<Tab>("CUSTOMS");
  const [operation, setOperation] = useState<OperationDetail | null>(null);
  const [summary, setSummary] = useState<FinancialSummary | null>(null);
  const [budget, setBudget] = useState<Budget | null>(null);
  const [settlement, setSettlement] = useState<Settlement | null>(null);
  const [funds, setFunds] = useState<Fund[]>([]);
  const [expenses, setExpenses] = useState<Expense[]>([]);
  const [taxItems, setTaxItems] = useState<TaxItem[]>([]);
  const [receiptExpense,setReceiptExpense]=useState<Expense|null>(null);
  const [receiptCounts,setReceiptCounts]=useState<Record<number,number>>({});
  const [services, setServices] = useState<Service[]>([]);
  const [collections, setCollections] = useState<Collection[]>([]);
  const [issuedLiquidation,setIssuedLiquidation]=useState<import("@/features/finance/finance.api").Liquidation|null>(null);
  const [refunds, setRefunds] = useState<Refund[]>([]);
  const [entry, setEntry] = useState<FinanceEntryKind | null>(null);
  const [actionRequest,setActionRequest]=useState<ActionRequest|null>(null);
  const [serviceEditing,setServiceEditing]=useState<Service|null>(null);
  const [serviceOpen,setServiceOpen]=useState(false);
  const [editing, setEditing] = useState<Editable | null>(null);
  const [paymentExpense, setPaymentExpense] = useState<Expense | null>(null);
  const [paymentRows, setPaymentRows] = useState<ExpensePaymentRecord[]>([]);
  const [paymentLoading, setPaymentLoading] = useState(false);
  const [paymentError, setPaymentError] = useState("");
  const [expenseId, setExpenseId] = useState<number | null>(null);
  const [collectionId, setCollectionId] = useState<number | null>(null);
  const [revision, setRevision] = useState(0);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const locked = settlement?.status === "APPROVED";
  const canEdit = canManage && !locked;
  // La cobranza permanece operativa tras validar y emitir la liquidación.
  const canCollect = canManage && operation?.operationalStatus !== "CLOSED";
  const money = (amount: number, currency?: string) => formatMoney(amount, currency || budget?.baseCurrencyCode || operation?.quotationCurrencySnapshot || "PEN");

  const load = useCallback(async () => {
    setLoading(true); setError("");
    try {
      const [o, s, b, st, f, e, sv, c, r] = await Promise.all([
        operationsApi.get(operationId), financeApi.summary(operationId), financeApi.budget(operationId),
        financeApi.settlement(operationId), financeApi.funds(operationId), financeApi.expenses(operationId),
        financeApi.services(operationId), financeApi.collections(operationId), financeApi.refunds(operationId)
      ]);
      setOperation(o); setSummary(s); setBudget(b); setSettlement(st);
      setFunds(f); setExpenses(e); setServices(sv); setCollections(c); setRefunds(r);
      try { setIssuedLiquidation(await liquidationApi.issued(operationId)); } catch { setIssuedLiquidation(null); }
      try { const taxes = await customsTaxesApi.get(operationId); setTaxItems(taxes.items); } catch { setTaxItems([]); }
      if (user?.userType === "INTERNAL" && hasPermission(Permissions.FinanceManage)) {
        const counts = await Promise.all(e.map(async x=>{
          try {const receipts=await expenseReceiptsApi.list(operationId,x.operationExpenseId);return [x.operationExpenseId,receipts.filter(r=>!r.isVoid).length] as const}
          catch {return [x.operationExpenseId,-1] as const}
        }));
        setReceiptCounts(Object.fromEntries(counts));
      }
      setRevision(v => v + 1);
    } catch (err) { setError(err instanceof Error ? err.message : "No se pudieron cargar los movimientos financieros."); }
    finally { setLoading(false); }
  }, [operationId,user?.userType,hasPermission]);
  useEffect(() => { void load(); }, [load]);

  const openEntry = (kind: FinanceEntryKind, item?: Editable) => {
    setEditing(item || null); setEntry(kind); setExpenseId(null); setCollectionId(null);
  };
  const closeEntry = () => { setEntry(null); setEditing(null); setExpenseId(null); setCollectionId(null); };
  const remove = (item: Editable) => {
    const kind = isFund(item) ? "anticipo" : isExpense(item) ? "gasto" : isCollection(item) ? "cobro" : "devolución";
    setActionRequest({title:`Anular ${kind}`,description:"Se conservará el historial de la operación. Esta acción no puede deshacerse directamente.",label:"Motivo de anulación (obligatorio)",required:true,danger:true,onConfirm:async(reason)=>{
      if(isFund(item))await financeApi.voidFund(operationId,item.clientFundId,item.rowVersion,reason);
      else if(isExpense(item))await financeApi.voidExpense(operationId,item.operationExpenseId,item.rowVersion,reason);
      else if(isCollection(item))await financeApi.voidCollection(operationId,item.clientCollectionId,item.rowVersion,reason);
      else if(isRefund(item))await financeApi.voidRefund(operationId,item.clientRefundId,item.rowVersion,reason);
      await load();
    }});
  };
  const removeService=(item:Service)=>setActionRequest({title:"Anular servicio R&B",description:"El servicio se excluirá de la liquidación. No se permite si tiene cobros aplicados.",label:"Motivo de anulación (obligatorio)",required:true,danger:true,onConfirm:async(reason)=>{await financeApi.voidService(operationId,item.operationServiceId,item.rowVersion,reason);await load();}});
  const viewPayments = async (expense:Expense) => {
    setPaymentExpense(expense);setPaymentRows([]);setPaymentError("");setPaymentLoading(true);
    try {setPaymentRows(await financeApi.expensePayments(operationId,expense.operationExpenseId));}
    catch(e){setPaymentError(e instanceof Error?e.message:"No se pudieron consultar los pagos.");}
    finally{setPaymentLoading(false);}
  };
  const voidPayment = (p:ExpensePaymentRecord) => {
    if(!paymentExpense)return;
    const expense=paymentExpense;
    setActionRequest({title:"Anular pago a tercero",description:"El pago quedará anulado en el historial y se recalculará el saldo del gasto.",label:"Motivo de anulación",required:true,danger:true,onConfirm:async(reason)=>{
      await financeApi.voidExpensePayment(operationId,expense.operationExpenseId,p.expensePaymentId,p.rowVersion,reason);
      setPaymentRows(await financeApi.expensePayments(operationId,expense.operationExpenseId));
      await load();
    }});
  };
  const prepare = async () => {
    setBusy(true); setError("");
    try { await financeApi.prepareSettlement(operationId); await load(); }
    catch (err) { setError(err instanceof Error ? err.message : "No se pudo preparar la rendición."); }
    finally { setBusy(false); }
  };
  const approve = () => {
    if(!settlement)return;
    setActionRequest({title:"Confirmar validación",description:"Los saldos pendientes de cobro seguirán registrados y no se marcarán como pagados.",onConfirm:async()=>{await financeApi.approveSettlement(operationId,settlement.rowVersion);await load();}});
  };

  if (loading && !operation) return <Skeleton className="h-96" />;
  if (!operation || !summary) return <InlineAlert message={error || "Operación no disponible."} />;
  const activeFunds = funds.filter(x => x.status !== "CANCELLED");
  const activeExpenses = expenses.filter(x => x.status !== "CANCELLED");
  const activeCollections = collections.filter(x => x.status !== "VOID");
  const activeRefunds = refunds.filter(x => x.status !== "CANCELLED");
  const recordsToReview = activeExpenses.filter(x => x.isRebillable && !["COMPANY", "CLIENT"].includes(x.payerType ?? "" )).length + taxItems.filter(x => !["COMPANY", "CLIENT"].includes(x.payerType)).length;
  const action = (item: Editable, kind: FinanceEntryKind, disabled = false) => canEdit && !disabled ? (
    <div className="flex items-center gap-1">
      <button type="button" className="rounded-lg p-2 text-blue-700 hover:bg-blue-50" title="Editar" aria-label="Editar" disabled={busy} onClick={() => openEntry(kind, item)}><Pencil className="size-4" /></button>
      <button type="button" className="rounded-lg p-2 text-red-600 hover:bg-red-50" title="Anular" aria-label="Anular" disabled={busy} onClick={() => void remove(item)}><Trash2 className="size-4" /></button>
    </div>
  ) : <span className="text-xs text-slate-400">{disabled ? "Pago registrado: consulta y corrige el pago antes de anular el gasto" : "Bloqueado"}</span>;
  return <div className="space-y-3">
    <button className="inline-flex items-center gap-2 text-sm font-semibold text-slate-600" onClick={() => navigate("/finanzas")}><ArrowLeft className="size-4" />Volver a finanzas</button>
    <PageHeader eyebrow="Control económico de importación" title={operation.operationNo} description={`${operation.clientName} · ${operation.currentStageName}`} actions={<Button variant="secondary" onClick={() => void load()}><RefreshCcw className="size-4" />Actualizar</Button>} />
    {error && <InlineAlert message={error} />}
    <div className="grid gap-2 md:grid-cols-3">
      <Card className="p-4"><div className="text-xs text-slate-500">Gastos registrados (moneda base)</div><div className="mt-1 text-xl font-bold">{money(summary.totalExpenses)}</div></Card>
      <Card className="p-4"><div className="text-xs text-slate-500">Servicios propios (moneda base)</div><div className="mt-1 text-xl font-bold">{money(summary.totalServices)}</div></Card>
      <Card className="p-4"><div className="text-xs text-slate-500">Cobranza de liquidación</div><div className="mt-1 text-xl font-bold">{issuedLiquidation?.currencySections?.map(x=>`${x.currencyCode} ${Math.max(0,-x.balance-activeCollections.filter(c=>c.type==="LIQUIDATION"&&c.currencyCode===x.currencyCode).reduce((a,c)=>a+c.amount,0)).toFixed(2)}`).join(" · ")??"Sin emitir"}</div></Card>
    </div>
    {canEdit && <Card className="px-3 py-3"><div className="flex flex-wrap items-center gap-2"><span className="mr-2 text-xs font-semibold uppercase tracking-wide text-slate-500">Acciones rápidas</span><Button variant="secondary" onClick={()=>openEntry("FUND")}><Plus className="size-4"/>Anticipo</Button><Button variant="secondary" onClick={()=>openEntry("EXPENSE")}><Plus className="size-4"/>Gasto de tercero</Button><Button variant="secondary" onClick={()=>{setServiceEditing(null);setServiceOpen(true)}}><Plus className="size-4"/>Servicio R&B</Button><Button variant="secondary" onClick={()=>openEntry("COLLECTION")}><Plus className="size-4"/>Cobro</Button><Button variant="secondary" onClick={()=>openEntry("REFUND")}><Plus className="size-4"/>Devolución</Button></div></Card>}
    <Tabs value={tab} onChange={v => setTab(v as Tab)} items={[{value:"COSTS",label:"Costos y servicios"},{value:"MOVEMENTS",label:"Fondos"},{value:"CUSTOMS",label:"Aduanas · PEN"},{value:"FINANCING",label:"Cobranza"},{value:"LIQUIDATION",label:"Liquidación final"}]} />
    {tab === "COSTS" && <div className="space-y-4">
      <Card className="overflow-hidden">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b p-5"><div><h2 className="font-semibold">Gastos de terceros</h2><p className="text-xs text-slate-500">Facturas y gastos asociados a la importación. Se conserva la moneda original de cada concepto.</p></div>{canEdit && <Button onClick={() => openEntry("EXPENSE")}><Plus className="size-4" />Agregar gasto</Button>}</div>
        <div className="divide-y">{activeExpenses.length ? activeExpenses.map(x => <div key={x.operationExpenseId} className="flex items-center justify-between gap-3 p-4"><div><div className="font-semibold">{x.description}</div><div className="text-xs text-slate-500">{money(x.amount,x.currencyCode)} · {x.status} · Pagado (base): {money(x.paidBaseAmount)}</div></div><div className="flex items-center gap-2">{canManage && <Button variant="secondary" onClick={()=>setReceiptExpense(x)}>{receiptCounts[x.operationExpenseId]===undefined?"Constancias":receiptCounts[x.operationExpenseId]<0?"Constancias · Error":receiptCounts[x.operationExpenseId]===0?"Adjuntar PDF":`${receiptCounts[x.operationExpenseId]} PDF · Ver`}</Button>}{action(x,"EXPENSE",x.paidBaseAmount > 0 || (receiptCounts[x.operationExpenseId]??0)>0)}</div></div>) : <p className="p-5 text-sm text-slate-500">No se han registrado gastos de terceros.</p>}</div>
      </Card>
      <Card className="overflow-hidden"><div className="flex items-center justify-between gap-3 border-b p-5"><div><h2 className="font-semibold">Servicios propios de R&B</h2><p className="text-xs text-slate-500">Importes definitivos facturados por R&B. Puedes agregar servicios no incluidos en la cotización.</p></div>{canEdit&&<Button onClick={()=>{setServiceEditing(null);setServiceOpen(true)}}><Plus className="size-4"/>Agregar servicio</Button>}</div><div className="divide-y">{services.filter(x=>x.status!=="CANCELLED").map(x=><div key={x.operationServiceId} className="flex items-center justify-between gap-3 p-4"><div><div className="font-medium">{x.description}</div><div className="text-xs text-slate-500">{x.status}</div></div><div className="flex items-center gap-3"><span className="font-semibold">{money(x.amount,x.currencyCode)}</span>{canEdit&&(x.collectedAmount>0?<span className="text-xs text-slate-500">Con cobros asociados</span>:<div className="flex gap-1"><button type="button" className="rounded-lg p-2 text-blue-700 hover:bg-blue-50" aria-label="Editar servicio" title="Editar servicio" onClick={()=>{setServiceEditing(x);setServiceOpen(true)}}><Pencil className="size-4"/></button><button type="button" className="rounded-lg p-2 text-red-600 hover:bg-red-50" aria-label="Anular servicio" title="Anular servicio" onClick={()=>removeService(x)}><Trash2 className="size-4"/></button></div>)}</div></div>)}{services.filter(x=>x.status!=="CANCELLED").length===0&&<p className="p-5 text-sm text-slate-500">Agrega los servicios efectivamente facturados por R&B.</p>}</div></Card>
      <Card className="p-5"><div className="flex flex-wrap items-center justify-between gap-3"><div><h2 className="font-semibold">Validación financiera</h2><p className="text-sm text-slate-500">Revisa responsables de desembolso y saldos antes de emitir. Los anticipos y los cobros no bloquean el registro ni la validación.</p></div><Badge tone={locked?"green":"amber"}>{settlement?.status??"Sin preparar"}</Badge></div><div className="mt-4 grid gap-3 sm:grid-cols-3"><SmallMetric label="Saldo de fondos (base)" value={money(summary.managedFundBalance)}/><SmallMetric label="Registros por revisar" value={String(recordsToReview)}/><SmallMetric label="Servicios por cobrar" value={money(summary.pendingServiceCollection)}/></div><div className="mt-4 flex flex-wrap gap-2">{canEdit && operation.currentStageCode==="SETTLEMENT" && <Button variant="secondary" loading={busy} onClick={()=>void prepare()}>Validar información</Button>}{canApprove && settlement && !locked && <Button loading={busy} onClick={()=>void approve()}><CheckCircle2 className="size-4" />Confirmar validación</Button>}</div></Card>

    </div>}
    {tab === "MOVEMENTS" && <div className="space-y-4">
      <Card className="overflow-hidden"><Section title="Anticipos recibidos del importador" description="Fondos administrados, principalmente para impuestos y pagos aduaneros." add={canEdit?()=>openEntry("FUND"):undefined} /><div className="divide-y">{activeFunds.map(x=><Line key={x.clientFundId} title={money(x.receivedAmount??0,x.currencyCode)} subtitle={x.referenceNo||"Sin referencia"} actions={action(x,"FUND")}/>)}{activeFunds.length===0&&<EmptyLine/>}</div></Card>
      <Card className="overflow-hidden"><Section title="Devoluciones al importador" description="Devolución de anticipos no utilizados." add={canEdit?()=>openEntry("REFUND"):undefined}/><div className="divide-y">{activeRefunds.map(x=><Line key={x.clientRefundId} title={money(x.amount,x.currencyCode)} subtitle={x.referenceNo||"Sin referencia"} actions={action(x,"REFUND")}/>)}{activeRefunds.length===0&&<EmptyLine/>}</div></Card>
      <Card className="p-5"><h2 className="font-semibold">Control de fondos (moneda base)</h2><div className="mt-3 grid gap-3 sm:grid-cols-3"><SmallMetric label="Anticipos" value={money(summary.totalClientFunds)}/><SmallMetric label="Pagos de gastos" value={money(summary.totalExpensePayments)}/><SmallMetric label="Saldo administrado" value={money(summary.managedFundBalance)}/></div><p className="mt-3 text-xs text-slate-500">Los saldos base son referenciales para conciliación. No reemplazan el desglose por moneda de los documentos originales.</p></Card>
    </div>}
    {tab === "CUSTOMS" && <CustomsTaxesPanel operationId={operationId} canEdit={canEdit} onChanged={load}/> }
    {tab === "FINANCING" && <div className="space-y-4">
      <Card className="p-5 space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3"><div><h2 className="font-semibold">Cobranza de liquidación</h2><p className="text-sm text-slate-500">La deuda se obtiene de la liquidación emitida. Los cobros se descuentan por moneda sin aplicaciones manuales.</p></div>{canCollect&&issuedLiquidation&&<Button onClick={()=>openEntry("COLLECTION")}>+ Registrar cobro</Button>}</div>
        {issuedLiquidation?.currencySections ? <div className="grid gap-3 sm:grid-cols-2">{issuedLiquidation.currencySections.map(section=>{
          const paid=activeCollections.filter(x=>x.type==="LIQUIDATION"&&x.currencyCode===section.currencyCode).reduce((a,x)=>a+x.amount,0);
          const pending=Math.max(0,-section.balance-paid);
          return <div key={section.currencyCode} className="rounded-xl bg-slate-50 p-4"><div className="text-xs text-slate-500">Saldo pendiente · {section.currencyCode}</div><div className="mt-1 text-xl font-bold">{money(pending,section.currencyCode)}</div><div className="mt-2 text-xs text-slate-500">Deuda al emitir: {money(Math.max(0,-section.balance),section.currencyCode)} · Cobros de liquidación: {money(paid,section.currencyCode)}</div></div>
        })}</div> : <p className="text-sm text-amber-700">Primero debe emitirse la liquidación para registrar su cobranza.</p>}
        <div className="border-t border-slate-100 pt-3"><h3 className="text-sm font-semibold">Cobros registrados</h3><div className="mt-2 divide-y">{activeCollections.map(x=><Line key={x.clientCollectionId} title={money(x.amount,x.currencyCode)} subtitle={`${x.type==="LIQUIDATION"?"Cobro de liquidación":`Registro anterior (${x.type})`} · ${x.referenceNo||"Sin referencia"}`}/>)}{activeCollections.length===0&&<EmptyLine/>}</div></div>
        {activeCollections.some(x=>x.type!=="LIQUIDATION")&&<p className="text-xs text-amber-700">Hay cobros anteriores al nuevo modelo. Sus aplicaciones deben conciliarse antes de considerarlos como abonos adicionales a la liquidación.</p>}
        <p className="text-xs text-slate-500">El vencimiento se calcula desde el envío confirmado de la liquidación. El pago anticipado permite cerrar antes del vencimiento.</p>
      </Card>
      <LiquidationIssuedPanel operationId={operationId} canApprove={canApprove} view="financing" revision={revision}/>
    </div>}
    {tab === "LIQUIDATION" && <LiquidationIssuedPanel operationId={operationId} canApprove={canApprove} revision={revision}/>}
    <Dialog open={paymentExpense!==null} onClose={()=>setPaymentExpense(null)} title="Pagos asociados al gasto" description={paymentExpense?.description} footer={<Button variant="secondary" onClick={()=>setPaymentExpense(null)}>Cerrar</Button>}>
      {paymentLoading&&<p className="text-sm text-slate-500">Consultando pagos...</p>}
      {paymentError&&<InlineAlert message={paymentError}/>}
      {!paymentLoading&&!paymentError&&<div className="divide-y">{paymentRows.map(p=><div key={p.expensePaymentId} className="flex items-center justify-between gap-3 py-3"><div><div className="font-medium">{money(p.amount,p.currencyCode)} · {p.payerType}</div><div className="text-xs text-slate-500">{p.paymentDate?.slice(0,10)} · {p.referenceNo||"Sin referencia"} {p.isVoid?"· Anulado":""}</div></div>{canEdit&&!p.isVoid&&<Button variant="secondary" onClick={()=>voidPayment(p)}>Anular pago</Button>}</div>)}{paymentRows.length===0&&<p className="text-sm text-slate-500">No hay pagos registrados.</p>}</div>}
    </Dialog>
    {canManage&&<ExpenseReceiptsDialog operationId={operationId} expense={receiptExpense} onClose={()=>setReceiptExpense(null)} onChanged={()=>void load()}/>}
    <ActionDialog request={actionRequest} onClose={()=>setActionRequest(null)}/>
    <ServiceEntryDialog open={serviceOpen} operationId={operationId} editing={serviceEditing} onClose={()=>setServiceOpen(false)} onSaved={async()=>{setServiceOpen(false);await load()}}/>
    <FinanceEntryDialog open={entry!==null} kind={entry} operationId={operationId} editing={editing} expenseId={expenseId} collectionId={collectionId} services={services} onClose={closeEntry} onSaved={async()=>{closeEntry();await load();}} />
  </div>;
}
function Section({title,description,add}:{title:string;description:string;add?:()=>void}){return <div className="flex flex-wrap items-center justify-between gap-3 border-b p-5"><div><h2 className="font-semibold">{title}</h2><p className="text-xs text-slate-500">{description}</p></div>{add&&<Button onClick={add}><Plus className="size-4"/>Agregar</Button>}</div>}
function Line({title,subtitle,actions}:{title:string;subtitle:string;actions:React.ReactNode}){return <div className="flex items-center justify-between gap-3 p-4"><div><div className="font-semibold">{title}</div><div className="text-xs text-slate-500">{subtitle}</div></div>{actions}</div>}
function SmallMetric({label,value}:{label:string;value:string}){return <div className="rounded-xl bg-slate-50 p-3"><div className="text-xs text-slate-500">{label}</div><div className="mt-1 font-semibold">{value}</div></div>}
function EmptyLine(){return <p className="p-5 text-sm text-slate-500">Sin movimientos registrados.</p>}
