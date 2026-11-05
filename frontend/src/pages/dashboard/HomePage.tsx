import {
  ArrowRight,
  CircleDollarSign,
  FileCheck2,
  FileWarning,
  PackageSearch,
  Workflow
} from "lucide-react";
import {
  useEffect,
  useState
} from "react";
import { Link } from "react-router-dom";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/layout/PageHeader";
import { useAuth } from "@/features/auth/AuthContext";
import { operationsApi } from "@/features/operations/operations.api";

const EMPTY = {
  total: 0,
  inProgress: 0,
  observed: 0,
  pendingSettlement: 0
};

export function HomePage() {
  const { user, hasPermission } = useAuth();
  const clientMode = user?.userType === "CLIENT";
  const canSeeOperations = hasPermission("OPERATIONS.VIEW");
  const [summary, setSummary] = useState(EMPTY);
  const [loading, setLoading] = useState(canSeeOperations);

  useEffect(() => {
    if (!canSeeOperations) return;

    setLoading(true);
    Promise.all([
      operationsApi.list({ page: 1, pageSize: 1 }),
      operationsApi.list({ operationalStatus: "IN_PROGRESS", page: 1, pageSize: 1 }),
      operationsApi.list({ documentaryStatus: "OBSERVED", page: 1, pageSize: 1 }),
      clientMode
        ? operationsApi.list({ documentaryStatus: "COMPLETE", page: 1, pageSize: 1 })
        : operationsApi.list({ financialStatus: "PENDING_SETTLEMENT", page: 1, pageSize: 1 })
    ])
      .then(([all, progress, observed, fourth]) => {
        setSummary({
          total: all.total,
          inProgress: progress.total,
          observed: observed.total,
          pendingSettlement: fourth.total
        });
      })
      .catch(() => setSummary(EMPTY))
      .finally(() => setLoading(false));
  }, [canSeeOperations, clientMode]);

  const cards = [
    {
      label: clientMode ? "Mis importaciones" : "Operaciones registradas",
      value: summary.total,
      detail: clientMode ? "de tu empresa" : "en R&B Freight Cargo S.A.C. · ImportOps",
      icon: PackageSearch
    },
    {
      label: "En proceso",
      value: summary.inProgress,
      detail: "estado operativo",
      icon: Workflow
    },
    {
      label: "Documentación observada",
      value: summary.observed,
      detail: "requiere atención",
      icon: FileWarning
    },
    clientMode
      ? {
          label: "Documentación completa",
          value: summary.pendingSettlement,
          detail: "operaciones al día",
          icon: FileCheck2
        }
      : {
          label: "Rendición pendiente",
          value: summary.pendingSettlement,
          detail: "estado financiero",
          icon: CircleDollarSign
        }
  ];

  return (
    <>
      <PageHeader
        eyebrow={clientMode ? "Portal cliente" : "Centro de operaciones"}
        title={`Hola, ${user?.firstName || user?.username}`}
        description={
          clientMode
            ? `Consulta información exclusivamente asociada a ${user?.clientName ?? "tu empresa"}.`
            : "Vista operativa basada en datos reales del backend. El dashboard gerencial se completará al cerrar los módulos transaccionales."
        }
      />

      {canSeeOperations && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {cards.map((item) => (
            <Card key={item.label} className="p-5">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <div className="text-sm font-medium text-slate-500">
                    {item.label}
                  </div>
                  <div className="mt-2 text-3xl font-bold tracking-tight text-slate-950">
                    {loading ? "—" : item.value}
                  </div>
                  <div className="mt-1 text-xs text-slate-500">
                    {item.detail}
                  </div>
                </div>
                <div className="grid size-10 place-items-center rounded-xl bg-blue-50 text-blue-700">
                  <item.icon className="size-5" />
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}

      <Card className={canSeeOperations ? "mt-5 overflow-hidden" : "overflow-hidden"}>
        <div className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-base font-semibold text-slate-900">
              {clientMode ? "Seguimiento de mis importaciones" : "Centro operativo"}
            </h2>
            <p className="mt-1 text-sm text-slate-500">
              {clientMode
                ? "El backend restringe cada consulta al cliente asociado a tu sesión."
                : "Accede a las operaciones y continúa con los módulos habilitados para tu perfil."}
            </p>
          </div>

          {canSeeOperations && (
            <Link
              to="/operaciones"
              className="inline-flex items-center gap-2 text-sm font-semibold text-blue-700 hover:text-blue-800"
            >
              {clientMode ? "Ver mis importaciones" : "Ver importaciones"}
              <ArrowRight className="size-4" />
            </Link>
          )}
        </div>
      </Card>
    </>
  );
}
