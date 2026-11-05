# Frontend V11.9 — Proforma / conceptos jerárquicos

Base: Frontend V11.8.1. Compatible con Backend V10.16.2 y BD V1.19.

## Cambios
- Se elimina la pestaña redundante `Conceptos` de la cotización. Los conceptos se gestionan exclusivamente dentro de `Proforma`.
- `Agregar/Editar concepto` usa navegación en cascada `Tipo → Categoría → Concepto`.
- Cambiar Tipo limpia Categoría y Concepto; cambiar Categoría limpia Concepto.
- El selector de Concepto se filtra por Tipo/Categoría cuando estos están seleccionados.
- También se permite buscar un concepto directamente sin elegir previamente Tipo/Categoría; al seleccionarlo se completa automáticamente su clasificación.
- La tarjeta `Configuración aplicada` confirma Tipo, Categoría e Impuesto y deja explícito que provienen del catálogo.
- Impuesto y clasificación dejan de editarse manualmente desde la proforma.
- La sección `Gastos y servicios de la proforma` conserva alta, edición y eliminación para no perder funcionalidad al retirar la pestaña Conceptos.
