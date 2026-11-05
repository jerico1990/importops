# Backend V10.8 — Import Operation Core

- Corrige GetCurrentUserDisplayNameAsync faltante en V10.7.
- OperationQuery soporta quotationId.
- GetOwnedTrackedAsync aplica client scope.
- Notes del detalle no se exponen a CLIENT.
- CLIENT no recibe dimensión FINANCIAL en status-history.
- Historial de cliente oculta comentarios/nombres internos.
- Tasks son internas: 403 para CLIENT.
- OperationTaskDto incluye AllowedNextStatuses calculados por backend.
- GET /api/operations/{id}/available-stages devuelve transiciones reales.
- Responsable operativo: INTERNAL activo, no system.*, rol ADMIN u OPERATIONS.
- Catálogos de estados, prioridades, notas, eventos y visibilidades vienen del backend.
- No requiere cambio de esquema SQL.
