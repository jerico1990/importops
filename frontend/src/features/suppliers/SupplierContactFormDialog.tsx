import {
  useEffect,
  useState,
  type FormEvent
} from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { HttpError } from "@/lib/api/http";
import { suppliersApi } from "./suppliers.api";
import type {
  SupplierContact,
  UpsertSupplierContactRequest
} from "./types";

export function SupplierContactFormDialog({
  open,
  supplierId,
  contact,
  onClose,
  onSaved
}: {
  open: boolean;
  supplierId: number;
  contact?: SupplierContact | null;
  onClose: () => void;
  onSaved: (contact: SupplierContact) => void;
}) {
  const editing = Boolean(contact);
  const [fullName, setFullName] = useState("");
  const [positionName, setPositionName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [isPrimary, setIsPrimary] = useState(false);
  const [isActive, setIsActive] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setFullName(contact?.fullName ?? "");
    setPositionName(contact?.positionName ?? "");
    setEmail(contact?.email ?? "");
    setPhone(contact?.phone ?? "");
    setIsPrimary(contact?.isPrimary ?? false);
    setIsActive(contact?.isActive ?? true);
    setError("");
  }, [open, contact]);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setError("");

    if (!fullName.trim()) {
      setError("El nombre del contacto es obligatorio.");
      return;
    }

    const request: UpsertSupplierContactRequest = {
      fullName: fullName.trim(),
      positionName: positionName.trim() || null,
      email: email.trim() || null,
      phone: phone.trim() || null,
      isPrimary,
      isActive
    };

    setSaving(true);
    try {
      const result = editing && contact
        ? await suppliersApi.updateContact(
            supplierId,
            contact.supplierContactId,
            request
          )
        : await suppliersApi.addContact(supplierId, request);

      onSaved(result);
    } catch (e) {
      setError(
        e instanceof HttpError
          ? e.message
          : "No se pudo guardar el contacto."
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open={open}
      title={editing ? "Editar contacto" : "Nuevo contacto"}
      description="Contacto comercial u operativo del proveedor."
      onClose={onClose}
      className="sm:max-w-2xl"
    >
      <form className="space-y-5" onSubmit={submit}>
        {error && <InlineAlert message={error} />}

        <div className="grid gap-4 sm:grid-cols-2">
          <label className="sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              Nombre completo *
            </span>
            <Input
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              required
              maxLength={160}
            />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              Cargo
            </span>
            <Input
              value={positionName}
              onChange={(e) => setPositionName(e.target.value)}
              placeholder="Sales Manager"
              maxLength={120}
            />
          </label>

          <label>
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              Teléfono
            </span>
            <Input
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              maxLength={50}
            />
          </label>

          <label className="sm:col-span-2">
            <span className="mb-1.5 block text-sm font-semibold text-slate-700">
              Email
            </span>
            <Input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              maxLength={200}
            />
          </label>
        </div>

        <div className="flex flex-wrap gap-5 rounded-xl bg-slate-50 px-4 py-3">
          <label className="flex items-center gap-2 text-sm text-slate-700">
            <input
              type="checkbox"
              checked={isPrimary}
              onChange={(e) => setIsPrimary(e.target.checked)}
              className="size-4 accent-blue-700"
            />
            Contacto principal
          </label>

          {editing && (
            <label className="flex items-center gap-2 text-sm text-slate-700">
              <input
                type="checkbox"
                checked={isActive}
                onChange={(e) => setIsActive(e.target.checked)}
                className="size-4 accent-blue-700"
              />
              Contacto activo
            </label>
          )}
        </div>

        <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancelar
          </Button>
          <Button type="submit" loading={saving}>
            Guardar contacto
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
