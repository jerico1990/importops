import {useState} from "react";
import {Dialog} from "@/components/ui/Dialog";
import {Button} from "@/components/ui/Button";
export type ActionRequest={title:string;description:string;label?:string;required?:boolean;danger?:boolean;onConfirm:(value:string)=>Promise<void>|void};
export function ActionDialog({request,onClose}:{request:ActionRequest|null;onClose:()=>void}){
 const [value,setValue]=useState("");const [busy,setBusy]=useState(false);const [error,setError]=useState("");
 const close=()=>{if(!busy){setValue("");setError("");onClose();}};
 const submit=async()=>{if(!request)return;if(request.required&&!value.trim()){setError("Este campo es obligatorio.");return;}setBusy(true);setError("");try{await request.onConfirm(value.trim());closeAfter();}catch(e){setError(e instanceof Error?e.message:"No se pudo completar la operación.");}finally{setBusy(false)}};
 const closeAfter=()=>{setValue("");setError("");onClose()};
 return <Dialog open={!!request} onClose={close} title={request?.title||"Confirmar"} description={request?.description} footer={<div className="flex justify-end gap-2"><Button variant="secondary" onClick={close} disabled={busy}>Cancelar</Button><Button onClick={()=>void submit()} disabled={busy||(request?.required&&!value.trim())} loading={busy}>{request?.danger?"Confirmar anulación":"Confirmar"}</Button></div>}>
 {request?.label&&<label className="block text-sm font-medium text-slate-700">{request.label}<textarea className="mt-2 w-full rounded-xl border border-slate-300 p-3" rows={3} value={value} onChange={e=>setValue(e.target.value)} placeholder="Escribe aquí..."/></label>}{error&&<p className="mt-2 text-sm text-red-700">{error}</p>}
 </Dialog>;
}
