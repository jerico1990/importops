# ImportOps Backend V10.11 — Alertas y notificaciones

Aplicar primero `database/ImportOpsDB_V1_14_ALERTS_NOTIFICATIONS.sql` sobre la base actual.

Incluye motor persistente de alertas, deduplicación, resolución automática, estado de lectura por usuario, aislamiento por cliente, prioridad al responsable y filtrado por permisos.

Endpoints: `GET /api/alerts`, `GET /api/alerts/summary`, `POST /api/alerts/{id}/read`, `POST /api/alerts/read-all`, `POST /api/alerts/{id}/dismiss`, y administración de reglas bajo `/api/alerts/rules`.

El motor se ejecuta cada 5 minutos y también reconcilia al consultar alertas. Las reglas de cliente están controladas por audiencia y por el tipo de alerta; las reglas exclusivamente internas no pueden cambiarse a CLIENT/BOTH desde administración.

## V10.11.2 - Alertas del flujo comercial y operativo

Se incorporan reglas reales para cubrir el inicio del proceso y decisiones de negocio que antes no generaban alerta:
- REQUEST_PENDING_REVIEW: solicitud enviada a revisión; interna y dirigida por REQUESTS.MANAGE.
- REQUEST_OBSERVED: solicitud observada; visible al cliente para subsanación.
- QUOTATION_REJECTED: rechazo del cliente; interna y dirigida por QUOTATIONS.MANAGE.
- QUOTATION_APPROVED_OPERATION_PENDING: cotización aprobada sin operación; interna y dirigida por OPERATIONS.MANAGE.
- OPERATION_ON_HOLD: operación en espera; cliente e interno mientras el estado continúe ON_HOLD.
- OPERATION_CANCELLED: cancelación; cliente e interno durante 72 horas y luego queda RESOLVED en historial.
- DOCUMENT_OBSERVED: se refuerza la regla existente para impedir que una observación de documento INTERNAL se exponga al cliente.

El borrador NEW de una solicitud no genera alerta. REQUEST_PENDING_REVIEW nace únicamente con UNDER_REVIEW y se resuelve automáticamente al cambiar el estado.
