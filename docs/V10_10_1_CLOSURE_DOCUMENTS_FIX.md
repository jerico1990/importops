# Backend V10.10.1 — Closure + Documentary Status Fix

Base acumulativa: V10.10 Customs + Finance Final.

Correcciones funcionales:
- El estado documental de una etapa sin requisitos obligatorios pasa a COMPLETE.
- Los documentos OBSERVED solo afectan el estado de la etapa cuando corresponden
  a tipos documentales obligatorios de dicha etapa.
- Documentos VALID vencidos ya no satisfacen requisitos.
- Al cambiar de etapa se recalcula automáticamente DocumentaryStatus para la
  nueva etapa.
- CloseAsync recalcula DocumentaryStatus antes de validar el cierre, evitando
  bloquear operaciones por un estado documental histórico/desactualizado.
- CLOSED continúa siendo una etapa final especial: no se expone como transición
  normal en available-stages; se alcanza exclusivamente por POST /close.
- Se conserva la validación de cierre: SETTLEMENT + tareas obligatorias completas
  + documentos completos + Finance CLOSED + transición SETTLEMENT -> CLOSED.

Patch de datos incluido:
database/ImportOpsDB_V1_13_SETTLEMENT_DOCUMENT_STATUS_FIX.sql

El patch:
- renombra SETTLEMENT de "Pendiente rendición" a "Rendición y cierre";
- recalcula DocumentaryStatus de operaciones existentes según su etapa actual;
- registra historial DOCUMENTARY cuando el estado cambia.
