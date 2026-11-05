# Frontend V9.1 — Closure UX Fix

Corrección específica de cierre:
- El botón "Avanzar etapa" se muestra solo si el backend devuelve al menos una
  transición normal en GET /api/operations/{id}/available-stages.
- En SETTLEMENT, donde CLOSED no es transición normal, ya no se abre un combo vacío.
- El botón "Cerrar" se muestra solo cuando la operación está DELIVERED y la etapa
  actual es SETTLEMENT.
- El backend sigue siendo la fuente de verdad y valida el cierre.
