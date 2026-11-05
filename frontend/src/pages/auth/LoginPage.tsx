import {
  ArrowRight,
  Eye,
  EyeOff,
  LockKeyhole,
  ShipWheel
} from "lucide-react";
import {
  useState,
  type FormEvent
} from "react";
import {
  Navigate,
  useLocation,
  useNavigate
} from "react-router-dom";
import { useAuth } from "@/features/auth/AuthContext";
import { Button } from "@/components/ui/Button";
import { InlineAlert } from "@/components/feedback/InlineAlert";
import { HttpError } from "@/lib/api/http";

export function LoginPage() {
  const { user, login, loginPending, checkingSession } =
    useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [username, setUsername] = useState("admin");
  const [password, setPassword] = useState("Admin123*");
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  if (!checkingSession && user) {
    return <Navigate to="/" replace />;
  }

  const onSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setErrorMessage("");

    try {
      await login({
        username: username.trim(),
        password,
        rememberMe
      });

      const from =
        (location.state as { from?: string } | null)?.from ??
        "/";
      navigate(from, { replace: true });
    } catch (error) {
      setErrorMessage(
        error instanceof HttpError
          ? error.message
          : "No se pudo iniciar sesión."
      );
    }
  };

  return (
    <div className="grid min-h-screen bg-white lg:grid-cols-[1.05fr_0.95fr]">
      <section className="relative hidden overflow-hidden bg-[#102947] lg:flex lg:flex-col lg:justify-between">
        <div className="absolute -right-24 -top-24 size-96 rounded-full bg-blue-500/10 blur-3xl" />
        <div className="absolute -bottom-40 -left-32 size-[30rem] rounded-full bg-cyan-300/[0.07] blur-3xl" />

        <div className="relative z-10 px-10 py-9 xl:px-14">
          <div className="flex items-center gap-3">
            <div className="grid size-11 place-items-center rounded-2xl bg-blue-600 text-white shadow-lg shadow-blue-950/30">
              <ShipWheel className="size-6" />
            </div>
            <div>
              <div className="text-xl font-bold leading-tight tracking-tight text-white">
                R&B Freight Cargo S.A.C.
              </div>
              <div className="mt-1 text-sm text-slate-300">
                ImportOps · Importaciones y Aduanas
              </div>
            </div>
          </div>
        </div>

        <div className="relative z-10 max-w-2xl px-10 pb-14 xl:px-14">
          <div className="text-xs font-bold uppercase tracking-[0.2em] text-blue-300">
            Operación centralizada
          </div>
          <h1 className="mt-4 max-w-xl text-4xl font-semibold leading-tight tracking-tight text-white xl:text-5xl">
            Cada importación, desde la solicitud hasta la rendición.
          </h1>
          <p className="mt-5 max-w-xl text-base leading-7 text-slate-300">
            Clientes, cotizaciones, embarques, aduanas, documentos y finanzas
            en un único expediente operativo.
          </p>

          <div className="mt-9 grid max-w-xl grid-cols-3 gap-3">
            {[
              ["Trazabilidad", "Timeline completo"],
              ["Control", "Workflow por etapas"],
              ["Acceso", "Perfiles por cliente"]
            ].map(([title, detail]) => (
              <div
                key={title}
                className="rounded-2xl border border-white/10 bg-white/[0.045] p-4"
              >
                <div className="text-sm font-semibold text-white">
                  {title}
                </div>
                <div className="mt-1 text-xs leading-5 text-slate-400">
                  {detail}
                </div>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="flex min-h-screen items-center justify-center bg-slate-50/60 px-5 py-10 sm:px-8">
        <div className="w-full max-w-[430px]">
          <div className="mb-8 flex items-center gap-3 lg:hidden">
            <div className="grid size-10 place-items-center rounded-xl bg-blue-700 text-white">
              <ShipWheel className="size-5" />
            </div>
            <div>
              <div className="font-bold text-slate-900">
                R&B Freight Cargo S.A.C.
              </div>
              <div className="text-xs text-slate-500">
                ImportOps · Gestión de importaciones
              </div>
            </div>
          </div>

          <div className="rounded-3xl border border-slate-200 bg-white p-6 shadow-xl shadow-slate-900/[0.045] sm:p-8">
            <div className="mb-7">
              <div className="mb-3 grid size-11 place-items-center rounded-2xl bg-blue-50 text-blue-700">
                <LockKeyhole className="size-5" />
              </div>
              <h2 className="text-2xl font-bold tracking-tight text-slate-950">
                Iniciar sesión
              </h2>
              <p className="mt-1.5 text-sm leading-6 text-slate-500">
                Ingresa con el usuario asignado a tu perfil.
              </p>
            </div>

            <form
              className="space-y-4"
              onSubmit={onSubmit}
            >
              {errorMessage && (
                <InlineAlert message={errorMessage} />
              )}

              <label className="block">
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">
                  Usuario o correo
                </span>
                <input
                  autoComplete="username"
                  className="focus-ring h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 text-sm shadow-sm placeholder:text-slate-400"
                  value={username}
                  onChange={(event) =>
                    setUsername(event.target.value)
                  }
                  placeholder="usuario"
                  required
                />
              </label>

              <label className="block">
                <span className="mb-1.5 block text-sm font-semibold text-slate-700">
                  Contraseña
                </span>
                <div className="relative">
                  <input
                    autoComplete="current-password"
                    type={showPassword ? "text" : "password"}
                    className="focus-ring h-11 w-full rounded-xl border border-slate-200 bg-white px-3.5 pr-11 text-sm shadow-sm placeholder:text-slate-400"
                    value={password}
                    onChange={(event) =>
                      setPassword(event.target.value)
                    }
                    required
                  />
                  <button
                    type="button"
                    className="absolute right-1.5 top-1/2 -translate-y-1/2 rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                    onClick={() =>
                      setShowPassword((current) => !current)
                    }
                    aria-label={
                      showPassword
                        ? "Ocultar contraseña"
                        : "Mostrar contraseña"
                    }
                  >
                    {showPassword ? (
                      <EyeOff className="size-4" />
                    ) : (
                      <Eye className="size-4" />
                    )}
                  </button>
                </div>
              </label>

              <label className="flex cursor-pointer items-center gap-2 text-sm text-slate-600">
                <input
                  type="checkbox"
                  className="size-4 rounded border-slate-300 accent-blue-700"
                  checked={rememberMe}
                  onChange={(event) =>
                    setRememberMe(event.target.checked)
                  }
                />
                Mantener sesión iniciada
              </label>

              <Button
                type="submit"
                loading={loginPending}
                className="mt-2 h-11 w-full rounded-xl"
              >
                Ingresar
                {!loginPending && (
                  <ArrowRight className="size-4" />
                )}
              </Button>
            </form>
          </div>

          <p className="mt-5 text-center text-xs leading-5 text-slate-400">
            Acceso restringido. Las acciones realizadas quedan sujetas a
            auditoría del sistema.
          </p>
        </div>
      </section>
    </div>
  );
}
