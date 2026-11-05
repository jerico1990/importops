import { ShieldX } from "lucide-react";
import { Link } from "react-router-dom";

export function ForbiddenPage() {
  return (
    <div className="grid min-h-[70vh] place-items-center px-4">
      <div className="max-w-md text-center">
        <div className="mx-auto grid size-14 place-items-center rounded-2xl bg-red-50 text-red-600">
          <ShieldX className="size-6" />
        </div>
        <h1 className="mt-5 text-2xl font-bold text-slate-950">
          Acceso restringido
        </h1>
        <p className="mt-2 text-sm leading-6 text-slate-500">
          Tu perfil no tiene permisos para visualizar este módulo.
        </p>
        <Link
          to="/"
          className="mt-6 inline-flex text-sm font-semibold text-blue-700 hover:text-blue-800"
        >
          Volver al inicio
        </Link>
      </div>
    </div>
  );
}
