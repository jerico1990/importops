import {
  Construction,
  ArrowRight
} from "lucide-react";
import { useNavigate } from "react-router-dom";
import { PageHeader } from "@/components/layout/PageHeader";
import { Card } from "@/components/ui/Card";
import { Button } from "@/components/ui/Button";

export function ModulePlaceholderPage({
  title,
  description,
  nextLabel
}: {
  title: string;
  description: string;
  nextLabel?: string;
}) {
  const navigate = useNavigate();

  return (
    <>
      <PageHeader
        eyebrow="R&B Freight Cargo S.A.C. · ImportOps"
        title={title}
        description={description}
      />

      <Card className="p-8 text-center sm:p-12">
        <div className="mx-auto grid size-14 place-items-center rounded-2xl bg-blue-50 text-blue-700">
          <Construction className="size-6" />
        </div>
        <h2 className="mt-5 text-lg font-semibold text-slate-900">
          Módulo preparado en la arquitectura
        </h2>
        <p className="mx-auto mt-2 max-w-lg text-sm leading-6 text-slate-500">
          La base de navegación, seguridad y layout ya está lista. Este módulo
          se implementará en el orden funcional acordado, reutilizando los
          mismos componentes y patrones.
        </p>

        {nextLabel && (
          <Button
            className="mt-6"
            onClick={() => navigate("/maestros/clientes")}
          >
            {nextLabel}
            <ArrowRight className="size-4" />
          </Button>
        )}
      </Card>
    </>
  );
}
