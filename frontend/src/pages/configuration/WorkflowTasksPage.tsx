import { useCallback, useEffect, useState } from "react";
import { Pencil, Plus, Power, Search, Trash2 } from "lucide-react";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { Badge } from "@/components/ui/Badge";
import { Dialog } from "@/components/ui/Dialog";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { EmptyState } from "@/components/feedback/EmptyState";
import { HttpError } from "@/lib/api/http";
import { useAuth } from "@/features/auth/AuthContext";
import { Permissions } from "@/types/auth";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type { WorkflowStage } from "@/features/catalogs/types";
import { WorkflowTaskTemplateDialog } from "@/features/workflow/WorkflowTaskTemplateDialog";
import { workflowConfigurationApi } from "@/features/workflow/workflow.api";
import type { WorkflowTaskTemplate, WorkflowTaskTemplateDeleteCheck } from "@/features/workflow/types";

export function WorkflowTasksPage() {
  const { hasPermission } = useAuth();
  const canManage = hasPermission(Permissions.ConfigurationManage);
  const [stages, setStages] = useState<WorkflowStage[]>([]);
  const [stageId, setStageId] = useState("");
  const [items, setItems] = useState<WorkflowTaskTemplate[]>([]);
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [status, setStatus] = useState("ALL");
  const [page, setPage] = useState(1);
  const [pages, setPages] = useState(1);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<WorkflowTaskTemplate | null>(null);
  const [deleting, setDeleting] = useState<WorkflowTaskTemplate | null>(null);
  const [deleteCheck, setDeleteCheck] = useState<WorkflowTaskTemplateDeleteCheck | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    catalogsApi.workflowStages()
      .then((data) => setStages([...data].sort((a, b) => a.displayOrder - b.displayOrder)))
      .catch(() => setError("No se pudieron cargar las etapas del workflow."));
  }, []);

  useEffect(() => {
    const t = setTimeout(() => {
      setAppliedSearch(search.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(t);
  }, [search]);

  const load = useCallback(async () => {
    setLoading(true);
    setError("");
    try {
      const result = await workflowConfigurationApi.taskTemplates({
        page,
        pageSize: 20,
        workflowStageId: stageId ? Number(stageId) : undefined,
        search: appliedSearch || undefined,
        status
      });
      setItems(result.items);
      setTotal(result.total);
      setPages(Math.max(1, result.totalPages));
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudieron cargar las tareas por etapa.");
    } finally {
      setLoading(false);
    }
  }, [page, stageId, appliedSearch, status]);

  useEffect(() => { void load(); }, [load]);
  useEffect(() => { setPage(1); }, [stageId, status]);

  const changeActive = async (item: WorkflowTaskTemplate) => {
    setBusy(true);
    setError("");
    try {
      await workflowConfigurationApi.setTaskTemplateActive(item.stageTaskTemplateId, !item.isActive);
      await load();
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudo cambiar el estado de la plantilla.");
    } finally {
      setBusy(false);
    }
  };

  const askDelete = async (item: WorkflowTaskTemplate) => {
    setDeleting(item);
    setDeleteCheck(null);
    try {
      setDeleteCheck(await workflowConfigurationApi.deleteCheck(item.stageTaskTemplateId));
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudo verificar el uso de la plantilla.");
      setDeleting(null);
    }
  };

  const remove = async () => {
    if (!deleting || !deleteCheck?.canDelete) return;
    setBusy(true);
    try {
      await workflowConfigurationApi.deleteTaskTemplate(deleting.stageTaskTemplateId);
      setDeleting(null);
      setDeleteCheck(null);
      await load();
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudo eliminar la plantilla.");
    } finally {
      setBusy(false);
    }
  };

  const deactivateFromDelete = async () => {
    if (!deleting) return;
    setBusy(true);
    try {
      await workflowConfigurationApi.setTaskTemplateActive(deleting.stageTaskTemplateId, false);
      setDeleting(null);
      setDeleteCheck(null);
      await load();
    } catch (err) {
      setError(err instanceof HttpError ? err.message : "No se pudo desactivar la plantilla.");
    } finally {
      setBusy(false);
    }
  };

  return (
    <>
      <PageHeader
        eyebrow="Administración · Configuración · Workflow"
        title="Tareas por etapa"
        description="Define las tareas que R&B Freight Cargo S.A.C. · ImportOps genera automáticamente cuando una operación ingresa a una etapa. Se asignan siempre al responsable de la operación."
        actions={canManage ? <Button onClick={() => { setEditing(null); setFormOpen(true); }}><Plus className="size-4" />Nueva tarea</Button> : undefined}
      />

      <Card className="overflow-hidden">
        <div className="grid gap-3 border-b border-slate-100 p-4 lg:grid-cols-[1.4fr_1fr_220px]">
          <div className="relative">
            <Search className="absolute left-3 top-3 size-4 text-slate-400" />
            <Input className="pl-9" value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Buscar por título o descripción..." />
          </div>
          <SearchableSelect
            value={stageId || undefined}
            onChange={(value) => setStageId(value ?? "")}
            options={[{ value: "", label: "Todas las etapas" }, ...stages.map((stage) => ({ value: String(stage.workflowStageId), label: `${stage.displayOrder}. ${stage.name}`, description: stage.code }))]}
            placeholder="Todas las etapas"
            searchPlaceholder="Buscar etapa..."
          />
          <select className="h-10 rounded-lg border border-slate-200 px-3 text-sm" value={status} onChange={(e) => setStatus(e.target.value)}>
            <option value="ALL">Todos</option>
            <option value="ACTIVE">Activos</option>
            <option value="INACTIVE">Inactivos</option>
          </select>
        </div>

        {error && <div className="p-4"><InlineAlert message={error} /></div>}
        {loading ? <div className="p-6 text-sm text-slate-500">Cargando tareas por etapa...</div> : items.length === 0 ? (
          <EmptyState title="Sin tareas configuradas" description="No existen plantillas de tarea para los filtros seleccionados." />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[980px]">
              <thead><tr className="bg-slate-50 text-left text-xs uppercase text-slate-500"><th className="px-4 py-3">Etapa</th><th className="px-4 py-3">Tarea</th><th className="px-4 py-3">Plazo</th><th className="px-4 py-3">Orden</th><th className="px-4 py-3">Tipo</th><th className="px-4 py-3">Uso</th><th className="px-4 py-3">Estado</th><th className="px-4 py-3">Acciones</th></tr></thead>
              <tbody>{items.map((item) => (
                <tr key={item.stageTaskTemplateId} className="border-t border-slate-100 align-top">
                  <td className="px-4 py-3"><div className="text-sm font-semibold text-slate-800">{item.stageName}</div><div className="text-xs text-slate-500">{item.stageCode}</div></td>
                  <td className="px-4 py-3"><div className="text-sm font-semibold text-slate-800">{item.title}</div>{item.description && <div className="mt-1 max-w-xl text-xs text-slate-500">{item.description}</div>}</td>
                  <td className="px-4 py-3 text-sm">{item.dueDaysOffset == null ? "Sin plazo" : `${item.dueDaysOffset} día(s)`}</td>
                  <td className="px-4 py-3 text-sm">{item.displayOrder}</td>
                  <td className="px-4 py-3"><Badge tone={item.isMandatory ? "blue" : "neutral"}>{item.isMandatory ? "Obligatoria" : "Opcional"}</Badge></td>
                  <td className="px-4 py-3 text-sm">{item.generatedTasksCount} tarea(s)</td>
                  <td className="px-4 py-3"><Badge tone={item.isActive ? "green" : "neutral"}>{item.isActive ? "Activo" : "Inactivo"}</Badge></td>
                  <td className="px-4 py-3"><div className="flex gap-1">{canManage && <><button className="rounded-lg p-2 hover:bg-slate-100" title="Editar" onClick={() => { setEditing(item); setFormOpen(true); }}><Pencil className="size-4" /></button><button disabled={busy} className="rounded-lg p-2 hover:bg-slate-100" title={item.isActive ? "Desactivar" : "Activar"} onClick={() => void changeActive(item)}><Power className="size-4" /></button><button className="rounded-lg p-2 text-red-600 hover:bg-red-50" title="Eliminar" onClick={() => void askDelete(item)}><Trash2 className="size-4" /></button></>}</div></td>
                </tr>
              ))}</tbody>
            </table>
          </div>
        )}
        <div className="flex items-center justify-between border-t border-slate-100 p-4 text-sm text-slate-500"><span>{total} registro(s) · Página {page} de {pages}</span><div className="flex gap-2"><Button variant="secondary" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Anterior</Button><Button variant="secondary" disabled={page >= pages} onClick={() => setPage((p) => p + 1)}>Siguiente</Button></div></div>
      </Card>

      <WorkflowTaskTemplateDialog open={formOpen} onClose={() => setFormOpen(false)} item={editing} stages={stages} initialStageId={stageId ? Number(stageId) : undefined} onSaved={() => void load()} />

      <Dialog
        open={!!deleting}
        onClose={() => { setDeleting(null); setDeleteCheck(null); }}
        title={deleteCheck?.canDelete ? "Eliminar definitivamente" : "No se puede eliminar esta plantilla"}
        description={deleting ? deleting.title : undefined}
        footer={<div className="flex justify-end gap-2"><Button variant="secondary" onClick={() => { setDeleting(null); setDeleteCheck(null); }}>Cancelar</Button>{deleteCheck?.canDelete ? <Button variant="danger" loading={busy} onClick={() => void remove()}>Eliminar definitivamente</Button> : deleteCheck ? <Button loading={busy} disabled={!deleting?.isActive} onClick={() => void deactivateFromDelete()}>Desactivar</Button> : null}</div>}
      >
        <p className="text-sm text-slate-700">{deleteCheck?.message ?? "Verificando referencias..."}</p>
        {deleteCheck && deleteCheck.generatedTasksCount > 0 && <div className="mt-4 rounded-xl bg-slate-50 p-4 text-sm"><span className="text-slate-500">Tareas históricas generadas:</span> <strong>{deleteCheck.generatedTasksCount}</strong></div>}
      </Dialog>
    </>
  );
}
