import { useEffect, useState, type FormEvent } from "react";
import { Dialog } from "@/components/ui/Dialog";
import { Button } from "@/components/ui/Button";
import { Input } from "@/components/ui/Input";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { clientsApi } from "./clients.api";
import type { ClientContact, CreateClientContactRequest } from "./types";
import { HttpError } from "@/lib/api/http";

export function ContactFormDialog({ open, clientId, contact, onClose, onSaved }: { open: boolean; clientId: number; contact?: ClientContact | null; onClose: () => void; onSaved: () => void }) {
  const [form, setForm] = useState<CreateClientContactRequest>({ fullName: "", positionName: "", email: "", phone: "", mobile: "", isPrimary: false });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!open) return;
    setError("");
    setForm(contact ? { fullName: contact.fullName, positionName: contact.positionName ?? "", email: contact.email ?? "", phone: contact.phone ?? "", mobile: contact.mobile ?? "", isPrimary: contact.isPrimary } : { fullName: "", positionName: "", email: "", phone: "", mobile: "", isPrimary: false });
  }, [open, contact]);

  const submit = async (e: FormEvent) => {
    e.preventDefault(); setError("");
    if (!form.fullName.trim()) return setError("El nombre del contacto es obligatorio.");
    setSaving(true);
    try {
      const payload = { ...form, fullName: form.fullName.trim(), positionName: form.positionName?.trim() || null, email: form.email?.trim() || null, phone: form.phone?.trim() || null, mobile: form.mobile?.trim() || null };
      if (contact) await clientsApi.updateContact(clientId, contact.clientContactId, { ...payload, isActive: contact.isActive });
      else await clientsApi.addContact(clientId, payload);
      onSaved(); onClose();
    } catch (err) { setError(err instanceof HttpError ? err.message : "No se pudo guardar el contacto."); }
    finally { setSaving(false); }
  };

  return (
    <Dialog open={open} onClose={onClose} title={contact ? "Editar contacto" : "Nuevo contacto"} description="Persona de contacto vinculada al cliente." footer={<div className="flex justify-end gap-2"><Button variant="secondary" onClick={onClose}>Cancelar</Button><Button form="contact-form" type="submit" loading={saving}>Guardar</Button></div>}>
      <form id="contact-form" onSubmit={submit} className="space-y-4">
        {error && <InlineAlert message={error} />}
        <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">Nombre completo *</span><Input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} /></label>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">Cargo</span><Input value={form.positionName ?? ""} onChange={(e) => setForm({ ...form, positionName: e.target.value })} /></label>
          <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">Correo</span><Input type="email" value={form.email ?? ""} onChange={(e) => setForm({ ...form, email: e.target.value })} /></label>
          <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">Teléfono</span><Input value={form.phone ?? ""} onChange={(e) => setForm({ ...form, phone: e.target.value })} /></label>
          <label className="block"><span className="mb-1.5 block text-sm font-semibold text-slate-700">Celular</span><Input value={form.mobile ?? ""} onChange={(e) => setForm({ ...form, mobile: e.target.value })} /></label>
        </div>
        <label className="flex cursor-pointer items-center gap-2 rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-3 text-sm text-slate-700"><input type="checkbox" className="size-4 accent-blue-700" checked={form.isPrimary} onChange={(e) => setForm({ ...form, isPrimary: e.target.checked })} /><span><strong>Contacto principal</strong><span className="block text-xs font-normal text-slate-500">Será el contacto preferido para esta empresa.</span></span></label>
      </form>
    </Dialog>
  );
}
