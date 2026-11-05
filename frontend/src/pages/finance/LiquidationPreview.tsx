import type { Collection, Expense, FinancialSummary, Fund, Refund, Service } from '@/features/finance/types';
import { formatMoney } from '@/lib/format';

type Props = {
  operationNo: string;
  clientName: string;
  currency: string;
  summary: FinancialSummary;
  funds: Fund[];
  expenses: Expense[];
  services: Service[];
  collections: Collection[];
  refunds: Refund[];
};

export function LiquidationPreview({ operationNo, clientName, currency, summary, funds, expenses, services, collections, refunds }: Props) {
  const money = (value: number) => formatMoney(value, currency);
  const activeExpenses = expenses.filter(x => x.status !== 'CANCELLED');
  const activeServices = services.filter(x => x.status !== 'CANCELLED');
  const activeFunds = funds.filter(x => x.status !== 'CANCELLED');
  const activeCollections = collections.filter(x => x.status !== 'VOID');
  const completedRefunds = refunds.filter(x => x.status === 'COMPLETED');
  const grossCost = summary.totalExpenses + summary.totalServices;
  const received = summary.totalClientFunds + summary.totalCollections;
  const netReceived = received - summary.totalRefunds;
  const balance = summary.clientBalance;
  const allSameCurrency = [...activeExpenses, ...activeServices, ...activeFunds, ...activeCollections, ...completedRefunds]
    .every(x => x.currencyCode === currency);
  const rows = [
    ...activeExpenses.map(x => ({ id: `e-${x.operationExpenseId}`, label: x.description, amount: x.baseAmount, type: 'Gasto' })),
    ...activeServices.map(x => ({ id: `s-${x.operationServiceId}`, label: x.description, amount: x.baseAmount, type: 'Servicio' }))
  ];
  return <div className="space-y-4">
    <div className="rounded-xl border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-900">
      <strong>Vista preliminar interna — no emitida.</strong> Los importes proceden de los registros financieros actuales. No constituye una liquidación definitiva ni un comprobante tributario.
    </div>
    <div className="rounded-xl border bg-white p-5 md:p-8">
      <div className="flex flex-wrap items-start justify-between gap-4 border-b pb-5">
        <div><p className="text-xs font-bold uppercase tracking-wider text-slate-500">R&B Freight Cargo S.A.C. · ImportOps · Liquidación comercial</p><h2 className="mt-2 text-xl font-bold">LIQUIDACIÓN FINAL DE IMPORTACIÓN</h2><p className="mt-1 text-sm text-slate-500">Operación {operationNo}</p></div>
        <span className="rounded-full bg-amber-100 px-3 py-1 text-xs font-bold text-amber-900">PRELIMINAR</span>
      </div>
      <div className="grid gap-3 border-b py-5 text-sm sm:grid-cols-2"><div><span className="text-slate-500">Señores</span><div className="font-semibold">{clientName}</div></div><div><span className="text-slate-500">Moneda base</span><div className="font-semibold">{currency}</div></div></div>
      {!allSameCurrency && <div className="my-4 rounded-lg bg-amber-50 p-3 text-sm text-amber-900">Existen movimientos en otra moneda. Los importes se muestran en valores base registrados; debe validarse la moneda base y los tipos de cambio antes de emitir.</div>}
      <h3 className="mb-3 mt-5 text-sm font-bold uppercase">Relación de gastos y servicios reales</h3>
      <div className="overflow-x-auto"><table className="w-full text-sm"><thead><tr className="border-b text-left text-xs uppercase text-slate-500"><th className="py-2">Concepto</th><th className="py-2">Tipo</th><th className="py-2 text-right">Importe base</th></tr></thead><tbody>{rows.map(r => <tr key={r.id} className="border-b"><td className="py-2 pr-4">{r.label}</td><td className="py-2 text-slate-500">{r.type}</td><td className="py-2 text-right tabular-nums">{money(r.amount)}</td></tr>)}</tbody></table></div>
      {!rows.length && <p className="py-4 text-sm text-slate-500">No existen gastos ni servicios registrados.</p>}
      <div className="mt-5 ml-auto max-w-lg space-y-2 text-sm">
        <Line label="Total gastos y servicios" amount={grossCost} money={money} strong />
        <Line label="Fondos recibidos del cliente" amount={summary.totalClientFunds} money={money}/>
        <Line label="Cobros recibidos por servicios" amount={summary.totalCollections} money={money}/>
        <Line label="Total recibido" amount={received} money={money} strong/>
        <Line label="Devoluciones realizadas" amount={summary.totalRefunds} money={money}/>
        <Line label="Recibido neto de devoluciones" amount={netReceived} money={money}/>
        <div className="border-t pt-3"><Line label={balance > 0.005 ? 'Saldo a favor del cliente' : balance < -0.005 ? 'Saldo pendiente de cobro' : 'Saldo conciliado'} amount={Math.abs(balance)} money={money} strong /></div>
      </div>
      {Math.abs(summary.clientBalance - (netReceived - grossCost)) > 0.01 && <p className="mt-4 rounded-lg bg-slate-100 p-3 text-sm text-slate-700">Existen ajustes financieros que afectan el saldo. Deben detallarse y validarse antes de la emisión.</p>}
      <p className="mt-6 border-t pt-4 text-xs text-slate-500">Los importes de esta vista se calculan a partir de los registros vigentes. Una futura emisión requerirá validación, numeración y una copia inmutable de los datos emitidos.</p>
    </div>
  </div>;
}
function Line({ label, amount, money, strong = false }: { label: string; amount: number; money: (n: number) => string; strong?: boolean }) {
  return <div className={`flex justify-between gap-4 ${strong ? 'font-bold' : ''}`}><span>{label}</span><span className="whitespace-nowrap tabular-nums">{money(amount)}</span></div>;
}
