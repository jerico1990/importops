import {
  useEffect,
  useState,
  type FormEvent
} from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { MoneyInput } from "@/components/ui/MoneyInput";
import { SearchableSelect } from "@/components/ui/SearchableSelect";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { catalogsApi } from "@/features/catalogs/catalogs.api";
import type {
  ExpenseCategory,
  QuotationItemTypeCatalog,
  ServiceCategory
} from "@/features/catalogs/types";
import { quotationsApi } from "./quotations.api";
import type { QuotationItem, QuotationConcept } from "./types";
import { HttpError } from "@/lib/api/http";

export function QuotationItemDialog({
  open,
  quotationId,
  item,
  onClose,
  onSaved
}: {
  open: boolean;
  quotationId: number;
  item?: QuotationItem | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const editing = Boolean(item);
  const [types, setTypes] = useState<QuotationItemTypeCatalog[]>([]);
  const [concepts, setConcepts] = useState<QuotationConcept[]>([]);
  const [quotationConceptId, setQuotationConceptId] = useState<string>();
  const [serviceCategories, setServiceCategories] = useState<ServiceCategory[]>([]);
  const [expenseCategories, setExpenseCategories] = useState<ExpenseCategory[]>([]);

  const [itemType, setItemType] = useState<string>();
  const [description, setDescription] = useState("");
  const [quantity, setQuantity] = useState("1");
  const [unitPrice, setUnitPrice] = useState("");
  const [discountAmount, setDiscountAmount] = useState("0");
  const [taxRate, setTaxRate] = useState("");
  const [serviceCategoryId, setServiceCategoryId] = useState<string>();
  const [expenseCategoryId, setExpenseCategoryId] = useState<string>();
  const [isRebillable, setIsRebillable] = useState(false);
  const [notes, setNotes] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    Promise.all([
      catalogsApi.quotationItemTypes(),
      catalogsApi.serviceCategories(),
      catalogsApi.expenseCategories(),
      quotationsApi.concepts()
    ])
      .then(([typeData, serviceData, expenseData, conceptData]) => {
        setTypes(typeData);
        setServiceCategories(serviceData);
        setExpenseCategories(expenseData);
        setConcepts(conceptData);
      })
      .catch(() => {
        setTypes([]);
        setServiceCategories([]);
        setExpenseCategories([]);
        setConcepts([]);
      });
  }, [open]);

  useEffect(() => {
    if (!open) return;
    setItemType(item?.itemType ?? undefined);
    setQuotationConceptId(item?.quotationConceptId ? String(item.quotationConceptId) : undefined);
    setDescription(item?.description ?? "");
    setQuantity(item?.quantity?.toString() ?? "1");
    setUnitPrice(item?.unitPrice?.toString() ?? "");
    setDiscountAmount(item?.discountAmount?.toString() ?? "0");
    setTaxRate(item?.taxRate?.toString() ?? "");
    setServiceCategoryId(item?.serviceCategoryId ? String(item.serviceCategoryId) : undefined);
    setExpenseCategoryId(item?.expenseCategoryId ? String(item.expenseCategoryId) : undefined);
    setIsRebillable(item?.isRebillable ?? false);
    setNotes(item?.notes ?? "");
    setError("");
  }, [open, item]);

  useEffect(() => {
    if (!itemType) return;

    const currentType = types.find((x) => x.code === itemType);
    if (currentType?.categorySource !== "SERVICE_CATEGORY") {
      setServiceCategoryId(undefined);
    }
    if (currentType?.categorySource !== "EXPENSE_CATEGORY") {
      setExpenseCategoryId(undefined);
      setIsRebillable(false);
    }
  }, [itemType, types]);

  useEffect(() => {
    if (!expenseCategoryId || editing) return;
    const category = expenseCategories.find((x) => x.expenseCategoryId === Number(expenseCategoryId));
    if (category) setIsRebillable(category.isRebillableDefault);
  }, [expenseCategoryId, expenseCategories, editing]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");

    const type = types.find((x) => x.code === itemType);

    if (!itemType || !description.trim()) {
      setError("Tipo y descripción son obligatorios.");
      return;
    }
    if (type?.categorySource === "SERVICE_CATEGORY" && !serviceCategoryId) {
      setError("Selecciona la categoría de servicio.");
      return;
    }
    if (type?.categorySource === "EXPENSE_CATEGORY" && !expenseCategoryId) {
      setError("Selecciona la categoría de gasto.");
      return;
    }
    if (!quotationConceptId) {
      setError("Selecciona un concepto de proforma.");
      return;
    }
    if (!quantity || Number(quantity) <= 0) {
      setError("La cantidad debe ser mayor a cero.");
      return;
    }
    if (unitPrice === "" || Number(unitPrice) < 0) {
      setError("Ingresa un precio unitario válido.");
      return;
    }
    if (taxRate === "" || Number(taxRate) < 0 || Number(taxRate) > 100) {
      setError("Ingresa una tasa de impuesto entre 0 y 100.");
      return;
    }

    const payload = {
      itemType,
      description: description.trim(),
      quantity: Number(quantity),
      unitPrice: Number(unitPrice),
      discountAmount: Number(discountAmount || 0),
      taxRate: Number(taxRate),
      expenseCategoryId: expenseCategoryId ? Number(expenseCategoryId) : null,
      serviceCategoryId: serviceCategoryId ? Number(serviceCategoryId) : null,
      isRebillable,
      notes: notes.trim() || null,
      quotationConceptId: quotationConceptId ? Number(quotationConceptId) : null
    };

    setSaving(true);
    try {
      if (editing && item) {
        await quotationsApi.updateItem(
          quotationId,
          item.quotationItemId,
          payload
        );
      } else {
        await quotationsApi.addItem(quotationId, payload);
      }
      onSaved();
    } catch (e) {
      setError(e instanceof HttpError ? e.message : "No se pudo guardar el concepto.");
    } finally {
      setSaving(false);
    }
  };

  const selectedType = types.find((x) => x.code === itemType);
  const selectedConcept = concepts.find((x) => String(x.quotationConceptId) === quotationConceptId);
  const selectedCategoryId = selectedType?.categorySource === "SERVICE_CATEGORY"
    ? serviceCategoryId
    : selectedType?.categorySource === "EXPENSE_CATEGORY"
      ? expenseCategoryId
      : undefined;
  const selectedCategoryName = selectedConcept?.serviceCategoryId
    ? serviceCategories.find((x) => x.serviceCategoryId === selectedConcept.serviceCategoryId)?.name
    : selectedConcept?.expenseCategoryId
      ? expenseCategories.find((x) => x.expenseCategoryId === selectedConcept.expenseCategoryId)?.name
      : undefined;

  const categoryOptions = selectedType?.categorySource === "SERVICE_CATEGORY"
    ? serviceCategories.map((x) => ({ value: String(x.serviceCategoryId), label: x.name, description: x.code }))
    : selectedType?.categorySource === "EXPENSE_CATEGORY"
      ? expenseCategories.map((x) => ({ value: String(x.expenseCategoryId), label: x.name, description: x.code }))
      : [];

  const visibleConcepts = concepts.filter((concept) => {
    if (!itemType) return true;
    const conceptCategorySource = concept.serviceCategoryId ? "SERVICE_CATEGORY" : concept.expenseCategoryId ? "EXPENSE_CATEGORY" : undefined;
    if (selectedType?.categorySource !== conceptCategorySource) return false;
    if (!selectedCategoryId) return true;
    return selectedType?.categorySource === "SERVICE_CATEGORY"
      ? String(concept.serviceCategoryId ?? "") === selectedCategoryId
      : String(concept.expenseCategoryId ?? "") === selectedCategoryId;
  });

  const applyConcept = (value?: string) => {
    setQuotationConceptId(value);
    setError("");
    const concept = concepts.find((x) => String(x.quotationConceptId) === value);
    if (!concept) return;

    setDescription(concept.name);
    setTaxRate(String(concept.isTaxable ? concept.defaultTaxRate : 0));
    setIsRebillable(concept.isRebillableDefault);
    if (concept.serviceCategoryId) {
      setItemType(types.find((x) => x.categorySource === "SERVICE_CATEGORY")?.code);
      setServiceCategoryId(String(concept.serviceCategoryId));
      setExpenseCategoryId(undefined);
    } else if (concept.expenseCategoryId) {
      setItemType(types.find((x) => x.categorySource === "EXPENSE_CATEGORY")?.code);
      setExpenseCategoryId(String(concept.expenseCategoryId));
      setServiceCategoryId(undefined);
    } else {
      setError("Este concepto aún no tiene categoría. Configúralo en Configuración → Catálogos → Conceptos de proforma.");
    }
  };

  const changeType = (value?: string) => {
    setItemType(value);
    setServiceCategoryId(undefined);
    setExpenseCategoryId(undefined);
    setQuotationConceptId(undefined);
    setError("");
  };

  const changeCategory = (value?: string) => {
    if (selectedType?.categorySource === "SERVICE_CATEGORY") setServiceCategoryId(value);
    if (selectedType?.categorySource === "EXPENSE_CATEGORY") setExpenseCategoryId(value);
    setQuotationConceptId(undefined);
    setError("");
  };

  return (
    <Dialog
      open={open}
      title={editing ? "Editar concepto" : "Agregar concepto"}
      description="Los importes y totales se recalculan en el backend."
      onClose={onClose}
      className="sm:max-w-3xl"
    >
      <form className="space-y-5" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        <div className="grid gap-4 sm:grid-cols-2">
          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Tipo *</span>
            <SearchableSelect
              value={itemType}
              onChange={changeType}
              options={types.map((x) => ({ value: x.code, label: x.name }))}
              placeholder="Seleccionar tipo"
            />
          </div>

          <div>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Categoría *</span>
            <SearchableSelect
              value={selectedCategoryId}
              onChange={changeCategory}
              options={categoryOptions}
              placeholder={itemType ? "Seleccionar categoría" : "Primero selecciona un tipo"}
            />
          </div>

          <div className="sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Concepto de proforma *</span>
            <SearchableSelect
              value={quotationConceptId}
              onChange={applyConcept}
              options={visibleConcepts.map((x) => ({
                value: String(x.quotationConceptId),
                label: x.name,
                description: `${x.groupName} · ${x.isTaxable ? `IGV ${x.defaultTaxRate}%` : "No gravado"}`
              }))}
              placeholder={itemType ? (selectedCategoryId ? "Seleccionar concepto" : "Selecciona una categoría o busca un concepto") : "Buscar concepto o navegar por tipo y categoría"}
              searchPlaceholder="Buscar concepto..."
            />
            <p className="mt-1.5 text-xs text-slate-500">Puedes navegar Tipo → Categoría → Concepto o buscar directamente el concepto; al elegirlo se completa su clasificación.</p>
          </div>

          {selectedConcept && (
            <div className="sm:col-span-2 rounded-xl border border-slate-200 bg-slate-50 p-4">
              <div className="mb-3 flex items-center gap-2 text-xs font-bold uppercase tracking-wide text-slate-600">
                <span className="grid size-5 place-items-center rounded-full bg-emerald-100 text-emerald-700">✓</span>
                Configuración aplicada
              </div>
              <div className="grid gap-3 text-sm sm:grid-cols-3">
                <div><span className="block text-xs text-slate-500">Tipo</span><strong>{selectedConcept.serviceCategoryId ? "Servicio" : selectedConcept.expenseCategoryId ? "Gasto" : "Sin configurar"}</strong></div>
                <div><span className="block text-xs text-slate-500">Categoría</span><strong>{selectedCategoryName ?? "Pendiente de configuración"}</strong></div>
                <div><span className="block text-xs text-slate-500">Impuesto</span><strong>{selectedConcept.isTaxable ? `IGV ${selectedConcept.defaultTaxRate}%` : "No gravado"}</strong></div>
              </div>
              <p className="mt-3 border-t border-slate-200 pt-3 text-xs text-slate-500">Estos valores provienen del catálogo del concepto y no se editan desde la proforma.</p>
            </div>
          )}

          <label className="sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Descripción *</span>
            <Input
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Concepto que será presentado en la cotización."
              required
            />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Cantidad *</span>
            <Input type="number" min="0.0001" step="0.0001" value={quantity} onChange={(e) => setQuantity(e.target.value)} />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Precio unitario *</span>
            <MoneyInput min="0" value={unitPrice} onValueChange={setUnitPrice} />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Descuento</span>
            <MoneyInput min="0" value={discountAmount} onValueChange={setDiscountAmount} />
          </label>

          <label className="sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">Notas del concepto</span>
            <textarea
              className="focus-ring min-h-20 w-full resize-y rounded-xl border border-slate-200 px-3 py-2.5 text-sm"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </label>
        </div>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>Cancelar</Button>
          <Button type="submit" loading={saving}>Guardar concepto</Button>
        </div>
      </form>
    </Dialog>
  );
}
