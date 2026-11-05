# Backend V10.7 — Quotation

- CLIENT solo ve cotizaciones con SentAt != null y de su ClientId.
- Query ClientId ajeno -> 403.
- Detail ajeno o nunca enviado -> 404.
- InternalNotes se ocultan a CLIENT.
- Reject usa QUOTATIONS.APPROVE, igual que Approve.
- CLIENT_ADMIN recibe QUOTATIONS.APPROVE mediante SQL V1.11.
- Approval de CLIENT registra automáticamente la identidad de la sesión.
- GetOwnedTracked aplica client scope.
- CLIENT no puede crear/versionar aunque manipule permisos/request.
- Nueva versión solo desde REJECTED / EXPIRED / CANCELLED.
- SENT debe cerrarse antes de generar otra versión.
- Catálogos quotation-statuses y quotation-item-types.
